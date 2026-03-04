using System.Net.Http.Headers;

namespace Connectivity.Services
{
    public interface IGitHubService
    {
        string BuildRepoApiUrl(string repoOrUrl);

        Task GetRepoInfoAsync(string repoOrUrl, string pat, CancellationToken ct = default);
    }

    public sealed class GitHubService : IGitHubService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string BuildRepoApiUrl(string repoOrUrl)
        {
            if (string.IsNullOrWhiteSpace(repoOrUrl))
                throw new ArgumentException("repoOrUrl is required.", nameof(repoOrUrl));

            static string StripGitSuffix(string s) =>
                s.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;

            string ownerRepo;

            if (repoOrUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(repoOrUrl);
                var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                    throw new ArgumentException("Invalid GitHub repository URL.");

                ownerRepo = $"{segments[0]}/{StripGitSuffix(segments[1])}";
            }
            else
            {
                ownerRepo = StripGitSuffix(repoOrUrl.Trim());
                if (!ownerRepo.Contains('/'))
                    throw new ArgumentException("Use owner/repo or full GitHub URL.");
            }

            return $"https://api.github.com/repos/{ownerRepo}";
        }

        public async Task GetRepoInfoAsync(string repoOrUrl, string pat, CancellationToken ct = default)
        {
            var apiUrl = BuildRepoApiUrl(repoOrUrl);

            var client = _httpClientFactory.CreateClient("github");

            using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("token", pat);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            req.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

            using var resp = await client.SendAsync(req, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"GitHub call failed ({(int)resp.StatusCode}): {body}");
        }
    }
}