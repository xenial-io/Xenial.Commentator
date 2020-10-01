using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Appy.GitDb;
using Appy.GitDb.Local;
using Appy.GitDb.Core.Interfaces;
using Appy.GitDb.Core.Model;
using LibGit2Sharp;
using System.IO;

namespace Xenial.Commentator.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        // The Web API will only accept tokens 1) for users, and 2) having the "access_as_user" scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "access_as_user" };

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var creds = new UsernamePasswordCredentials
            {
                Username = "API_KEY",
                Password = string.Empty
            }; 

            var repoPath = Repository.Clone(@"https://github.com/biohazard999/TestingDb.git", dir, new CloneOptions
            {
                IsBare = true,
                CredentialsProvider = (_url, _user, _cred) => creds
            });

            // 1. Instantiate a new instance of the local git database
            IGitDb db = new LocalGitDb(repoPath);

            // 2. Save an object to the database
            var myObject = new SomeClass
            {
                SomeProperty = $"SomeValue{Guid.NewGuid()}"
            };

            await db.Save("master", "commit message", new Document<SomeClass> { Key = "key", Value = myObject }, new Author("Manuel Grundner", "m.grundner@delegate.at"));

            // 3. Retrieve the object
            var theObject = await db.Get<SomeClass>("master", "key");

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes["origin"];
                var options = new PushOptions();
                options.CredentialsProvider = (_url, _user, _cred) => creds;
                repo.Network.Push(remote, @"refs/heads/master", options);
            }

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = theObject.SomeProperty
            })
            .ToArray();
        }
    }

    public class SomeClass
    {
        public string SomeProperty { get; set; }
    }
}
