using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Infrastructure.Services;

public class GitLabService : IGitPlatformService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitLabService> _logger;
    private const string DefaultApiBaseUrl = "https://gitlab.com/api/v4";

    public GitPlatformType Platform => GitPlatformType.GitLab;

    public GitLabService(HttpClient httpClient, ILogger<GitLabService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private void SetupHeaders(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", accessToken);
    }

    private static string EncodeProjectPath(string fullName)
    {
        return Uri.EscapeDataString(fullName);
    }

    public async Task<PullRequestInfo> GetPullRequestAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var projectPath = EncodeProjectPath(repositoryFullName);
        var url = $"{baseUrl}/projects/{projectPath}/merge_requests/{pullRequestNumber}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var mr = JsonSerializer.Deserialize<GitLabMergeRequest>(content, JsonOptions);

        return new PullRequestInfo
        {
            Number = mr!.Iid,
            Title = mr.Title ?? string.Empty,
            Description = mr.Description ?? string.Empty,
            Author = mr.Author?.Username ?? string.Empty,
            SourceBranch = mr.SourceBranch ?? string.Empty,
            TargetBranch = mr.TargetBranch ?? string.Empty,
            State = mr.State ?? string.Empty,
            HeadCommitSha = mr.Sha ?? string.Empty,
            CreatedAt = mr.CreatedAt,
            UpdatedAt = mr.UpdatedAt
        };
    }

    public async Task<IEnumerable<PullRequestFile>> GetPullRequestFilesAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var projectPath = EncodeProjectPath(repositoryFullName);
        var url = $"{baseUrl}/projects/{projectPath}/merge_requests/{pullRequestNumber}/diffs";

        var allFiles = new List<GitLabMergeRequestDiff>();
        var page = 1;
        const int perPage = 100;

        while (true)
        {
            var pagedUrl = $"{url}?page={page}&per_page={perPage}";
            var response = await _httpClient.GetAsync(pagedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var files = JsonSerializer.Deserialize<List<GitLabMergeRequestDiff>>(content, JsonOptions);

            if (files == null || files.Count == 0)
                break;

            allFiles.AddRange(files);

            if (files.Count < perPage)
                break;

            page++;
        }

        return allFiles.Select(f => new PullRequestFile
        {
            FileName = f.NewPath ?? f.OldPath ?? string.Empty,
            ChangeType = ParseChangeType(f),
            Additions = 0, // GitLab doesn't provide this directly in diffs endpoint
            Deletions = 0,
            Patch = f.Diff
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
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var projectPath = EncodeProjectPath(repositoryFullName);

        // 先取得 MR 資訊獲取 diff refs
        var mr = await GetPullRequestAsync(repositoryFullName, pullRequestNumber, accessToken, apiBaseUrl, cancellationToken);

        var url = $"{baseUrl}/projects/{projectPath}/merge_requests/{pullRequestNumber}/discussions";

        var payload = new
        {
            body = comment,
            position = new
            {
                base_sha = mr.HeadCommitSha,
                start_sha = mr.HeadCommitSha,
                head_sha = mr.HeadCommitSha,
                position_type = "text",
                new_path = filePath,
                new_line = line
            }
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
            _logger.LogWarning(ex, "Failed to post discussion on line {Line}, trying MR note instead", line);
            await PostPullRequestCommentAsync(repositoryFullName, pullRequestNumber,
                $"**{filePath}** (line {line}):\n\n{comment}", accessToken, apiBaseUrl, cancellationToken);
        }
    }

    public async Task PostPullRequestCommentAsync(string repositoryFullName, int pullRequestNumber, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var projectPath = EncodeProjectPath(repositoryFullName);
        var url = $"{baseUrl}/projects/{projectPath}/merge_requests/{pullRequestNumber}/notes";

        var payload = new { body = comment };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        // GitLab 使用 X-Gitlab-Token header 進行簡單的 token 比對
        return string.Equals(signature, secret, StringComparison.Ordinal);
    }

    private static FileChangeType ParseChangeType(GitLabMergeRequestDiff diff)
    {
        if (diff.NewFile) return FileChangeType.Added;
        if (diff.DeletedFile) return FileChangeType.Deleted;
        if (diff.RenamedFile) return FileChangeType.Renamed;
        return FileChangeType.Modified;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private class GitLabMergeRequest
    {
        public int Iid { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? State { get; set; }
        public string? SourceBranch { get; set; }
        public string? TargetBranch { get; set; }
        public string? Sha { get; set; }
        public GitLabUser? Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private class GitLabUser
    {
        public string? Username { get; set; }
        public long Id { get; set; }
    }

    private class GitLabMergeRequestDiff
    {
        public string? OldPath { get; set; }
        public string? NewPath { get; set; }
        public string? Diff { get; set; }
        public bool NewFile { get; set; }
        public bool DeletedFile { get; set; }
        public bool RenamedFile { get; set; }
    }
}
