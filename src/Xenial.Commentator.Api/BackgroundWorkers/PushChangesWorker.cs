using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Appy.GitDb.Core.Interfaces;
using Appy.GitDb.Core.Model;
using Appy.GitDb.Local;

using LibGit2Sharp;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Xenial.Commentator.Api.Model;
using Xenial.Commentator.Helpers;
using Xenial.Commentator.Model;

namespace Xenial.Commentator.BackgroundWorkers
{
    public class PushChangesWorker : IHostedService, IDisposable
    {
        private readonly ILogger<PushChangesWorker> _logger;
        private Timer _timer;
        private ConcurrentQueue<PageWorkModel> _queue;
        private IHttpClientFactory _httpClientFactory;
        private Lazy<string> repositoryLocation;

        public PushChangesWorker(ILogger<PushChangesWorker> logger, ConcurrentQueue<PageWorkModel> queue, IHttpClientFactory httpClientFactory)
            => (_logger, _queue, _httpClientFactory, repositoryLocation) = (logger, queue, httpClientFactory, new Lazy<string>(() => CloneRepository()));

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Push Changes Service is running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private string CloneRepository()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var repoPath = Repository.Clone(@"https://github.com/biohazard999/TestingDb.git", dir, new CloneOptions
            {
                IsBare = true
            });

            return repoPath;
        }

        private readonly string branchName = "master";

        private async void DoWork(object state)
        {
            if (_queue.TryDequeue(out var page))
            {
                try
                {
                    using IGitDb db = new LocalGitDb(repositoryLocation.Value);
                    var id = page.Id.TrimStart('/');
                    var key = $"comments/{id}";
                    var pageInDb = await db.Get<Page>(branchName, key);

                    if (pageInDb == null)
                    {
                        pageInDb = new Page { Id = id };
                    }
                    var client = _httpClientFactory.CreateClient(nameof(PushChangesWorker));


                    page.Comment.Content = StringHelper.StripMarkdownTags(page.Comment.Content);
                    page.Comment.AvatarUrl = await client.FetchAvatarFromGithub(_logger, page.Comment.GithubOrEmail);
                    page.Comment.GithubOrEmail = null;
                    if (string.IsNullOrWhiteSpace(page.Comment.Homepage))
                    {
                        page.Comment.Homepage = null;
                    }
                    pageInDb.Comments.Add(page.Comment);


                    await db.Save(branchName, $"feat: new comment in {page.Id}", new Document<Page>
                    {
                        Key = key,
                        Value = pageInDb
                    }, new Author("Manuel Grundner", "m.grundner@delegate.at"));

                    using var repo = new Repository(repositoryLocation.Value);

                    var creds = new UsernamePasswordCredentials
                    {
                        Username = Environment.GetEnvironmentVariable("GITHUB_API_KEY"),
                        Password = string.Empty
                    };

                    var remote = repo.Network.Remotes["origin"];
                    var options = new PushOptions();
                    options.CredentialsProvider = (_url, _user, _cred) => creds;
                    repo.Network.Push(remote, @"refs/heads/master", options);
                }
                catch (NonFastForwardException ex)
                {
                    _logger.LogWarning("Could not push changes cause there is a non fast forward. Clone the repo and try again. {page} {ex}", page, ex);
                    repositoryLocation = new Lazy<string>(() => CloneRepository());
                    _queue.Enqueue(page);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Could not commit comment in {page} {ex}", page, ex);
                    _queue.Enqueue(page);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Push Changes Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}