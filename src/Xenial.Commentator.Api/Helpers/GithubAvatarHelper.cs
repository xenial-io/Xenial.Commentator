

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xenial.Commentator.Api.Model;

namespace Xenial.Commentator.Helpers
{
    public static class GithubAvatarHelper
    {
        public static async Task<string> FetchAvatarFromGithub(this HttpClient client, ILogger _logger, string githubOrEmail)
        {
            if (!string.IsNullOrWhiteSpace(githubOrEmail))
            {
                githubOrEmail = githubOrEmail.Trim();
                try
                {
                    var assemlyName = typeof(GithubAvatarHelper).Assembly.GetName();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd($"{assemlyName.Name}/{assemlyName.Version}");

                    var response = await client.GetFromJsonAsync<GithubResponse>($"https://api.github.com/search/users?q={githubOrEmail}");

                    if (response.total_count > 0)
                    {
                        var githubUser = response.items.First();
                        return githubUser.avatar_url;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError("HttpRequestException: Error fetching avatar for {githubOrEmail}", githubOrEmail, ex);
                }
                catch (NotSupportedException ex)
                {
                    _logger.LogError("NotSupportedException: Error fetching avatar for {githubOrEmail}", githubOrEmail, ex);
                }
                catch (JsonException ex)
                {
                    _logger.LogError("JsonException: Error fetching avatar for {githubOrEmail}", githubOrEmail, ex);
                }
            }
            return null;
        }
    }
}