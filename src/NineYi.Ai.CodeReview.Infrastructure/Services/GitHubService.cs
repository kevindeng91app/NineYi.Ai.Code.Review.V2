using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Infrastructure.Services;

public class GitHubService : IGitPlatformService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;
    private const string DefaultApiBaseUrl = "https://api.github.com";

    public GitPlatformType Platform => GitPlatformType.GitHub;

    public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private void SetupHeaders(string accessToken, string? apiBaseUrl)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NineYi-CodeReview/1.0");
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<PullRequestInfo> GetPullRequestAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken, apiBaseUrl);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repos/{repositoryFullName}/pulls/{pullRequestNumber}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var pr = JsonSerializer.Deserialize<GitHubPullRequest>(content, JsonOptions);

        return new PullRequestInfo
        {
            Number = pr!.Number,
            Title = pr.Title ?? string.Empty,
            Description = pr.Body ?? string.Empty,
            Author = pr.User?.Login ?? string.Empty,
            SourceBranch = pr.Head?.Ref ?? string.Empty,
            TargetBranch = pr.Base?.Ref ?? string.Empty,
            State = pr.State ?? string.Empty,
            HeadCommitSha = pr.Head?.Sha ?? string.Empty,
            CreatedAt = pr.CreatedAt,
            UpdatedAt = pr.UpdatedAt
        };
    }

    public async Task<IEnumerable<PullRequestFile>> GetPullRequestFilesAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken, apiBaseUrl);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repos/{repositoryFullName}/pulls/{pullRequestNumber}/files";

        var allFiles = new List<GitHubPullRequestFile>();
        var page = 1;
        const int perPage = 100;

        while (true)
        {
            var pagedUrl = $"{url}?page={page}&per_page={perPage}";
            var response = await _httpClient.GetAsync(pagedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var files = JsonSerializer.Deserialize<List<GitHubPullRequestFile>>(content, JsonOptions);

            if (files == null || files.Count == 0)
                break;

            allFiles.AddRange(files);

            if (files.Count < perPage)
                break;

            page++;
        }

        return allFiles.Select(f => new PullRequestFile
        {
            FileName = f.Filename ?? string.Empty,
            ChangeType = ParseChangeType(f.Status),
            Additions = f.Additions,
            Deletions = f.Deletions,
            Patch = f.Patch,
            BlobUrl = f.BlobUrl
        });
    }

    public async Task<string> GetFileDiffAsync(string repositoryFullName, int pullRequestNumber, string filePath, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var files = await GetPullRequestFilesAsync(repositoryFullName, pullRequestNumber, accessToken, apiBaseUrl, cancellationToken);
        var file = files.FirstOrDefault(f => f.FileName == filePath);
        return file?.Patch ?? string.Empty;
    }

    public async Task PostReviewCommentAsync(string repositoryFullName, int pullRequestNumber, string filePath, int line, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken, apiBaseUrl);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;

        // 先取得 PR 的 commit SHA
        var pr = await GetPullRequestAsync(repositoryFullName, pullRequestNumber, accessToken, apiBaseUrl, cancellationToken);

        var url = $"{baseUrl}/repos/{repositoryFullName}/pulls/{pullRequestNumber}/comments";

        var payload = new
        {
            body = comment,
            commit_id = pr.HeadCommitSha,
            path = filePath,
            line = line,
            side = "RIGHT"
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to post review comment on line {Line}, trying PR comment instead", line);
            // 如果行號有問題，改用 PR 評論
            await PostPullRequestCommentAsync(repositoryFullName, pullRequestNumber,
                $"**{filePath}** (line {line}):\n\n{comment}", accessToken, apiBaseUrl, cancellationToken);
        }
    }

    public async Task PostPullRequestCommentAsync(string repositoryFullName, int pullRequestNumber, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken, apiBaseUrl);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repos/{repositoryFullName}/issues/{pullRequestNumber}/comments";

        var payload = new { body = comment };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            return false;

        var expectedSignature = signature.Substring(7);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var actualSignature = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(expectedSignature, actualSignature, StringComparison.OrdinalIgnoreCase);
    }

    private static FileChangeType ParseChangeType(string? status)
    {
        return status?.ToLower() switch
        {
            "added" => FileChangeType.Added,
            "modified" => FileChangeType.Modified,
            "removed" => FileChangeType.Deleted,
            "renamed" => FileChangeType.Renamed,
            _ => FileChangeType.Modified
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private class GitHubPullRequest
    {
        public int Number { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? State { get; set; }
        public GitHubUser? User { get; set; }
        public GitHubBranch? Head { get; set; }
        public GitHubBranch? Base { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private class GitHubUser
    {
        public string? Login { get; set; }
        public long Id { get; set; }
    }

    private class GitHubBranch
    {
        public string? Ref { get; set; }
        public string? Sha { get; set; }
    }

    private class GitHubPullRequestFile
    {
        public string? Filename { get; set; }
        public string? Status { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public string? Patch { get; set; }
        [JsonPropertyName("blob_url")]
        public string? BlobUrl { get; set; }
    }
}
