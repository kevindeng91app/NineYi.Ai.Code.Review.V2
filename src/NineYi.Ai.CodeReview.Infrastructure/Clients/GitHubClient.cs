using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineYi.Ai.CodeReview.Application.Abstractions;
using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.Models;
using NineYi.Ai.CodeReview.Application.Options;
using NineYi.Ai.CodeReview.Application.Utilities;

namespace NineYi.Ai.CodeReview.Infrastructure.Clients;

/// <summary>
/// GitHub REST API client，實作 <see cref="IRepoHostClient"/>。
/// 取 PR diff 清單：GET /repos/{owner}/{repo}/pulls/{number}/files
/// 取 raw content ：GET /repos/{owner}/{repo}/contents/{path}?ref={sha}
/// </summary>
public class GitHubClient : IRepoHostClient
{
    private readonly HttpClient _http;
    private readonly string _accessToken;
    private readonly FileExcludeOptions _excludeOptions;
    private readonly ILogger<GitHubClient> _logger;

    public GitHubClient(
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookSecretsOptions> secrets,
        IOptions<FileExcludeOptions> excludeOptions,
        ILogger<GitHubClient> logger)
    {
        _http = httpClientFactory.CreateClient("GitHub");
        _accessToken = secrets.Value.GitHub.AccessToken ?? string.Empty;
        _excludeOptions = excludeOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileDiffItem>> GetPullRequestDiffFilesAsync(
        StartCodeReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{command.RepoFullName}/pulls/{command.PullRequestNumber}/files";

        using var request = BuildRequest(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/vnd.github+json");

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var files = await response.Content.ReadFromJsonAsync<List<GitHubPrFile>>(cancellationToken: cancellationToken)
                    ?? [];

        var result = new List<FileDiffItem>();

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Patch))
                continue;

            var diffItem = new FileDiffItem
            {
                FilePath = file.Filename,
                FileExtension = Path.GetExtension(file.Filename),
                Diff = file.Patch,
                ChangedLineRanges = DiffHunkParser.Parse(file.Patch)
            };

            if (!IsExcluded(diffItem))
                result.Add(diffItem);
        }

        _logger.LogInformation(
            "GitHub: PR #{Number} in {Repo} — {Total} files fetched, {Kept} after filtering",
            command.PullRequestNumber, command.RepoFullName, files.Count, result.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetFileRawContentAsync(
        StartCodeReviewCommand command,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{command.RepoFullName}/contents/{filePath}" +
                  $"?ref={command.PullRequestRef.HeadCommitSha}";

        using var request = BuildRequest(HttpMethod.Get, url);
        // vnd.github.v3.raw → GitHub 直接回傳純文字，不包裝 base64
        request.Headers.Add("Accept", "application/vnd.github.v3.raw");

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    // ────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private bool IsExcluded(FileDiffItem item)
    {
        // 路徑前綴排除
        if (_excludeOptions.ExcludedPathPrefixes.Any(prefix =>
                item.FilePath.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        // 副檔名排除
        if (_excludeOptions.ExcludedFileExtensions.Any(ext =>
                item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    // TODO Phase 3 Step 5：實作 PostReviewCommentAsync
    public async Task PostReviewCommentAsync(
        StartCodeReviewCommand command,
        string filePath,
        int startLine,
        int endLine,
        string body,
        CancellationToken cancellationToken = default)
    {
        var parts = command.RepoFullName.Split('/');
        var owner = parts[0];
        var repo  = parts[1];
        var url   = $"https://api.github.com/repos/{owner}/{repo}/pulls/{command.PullRequestNumber}/comments";

        var payload = new
        {
            body      = body,
            commit_id = command.PullRequestRef.HeadCommitSha,
            path      = filePath,
            line      = startLine,
            side      = "RIGHT"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GitHub PostReviewComment failed: {Status} - {Error}", response.StatusCode, error);
        }
    }

    // TODO Phase 3 Step 5：實作 PostPullRequestCommentAsync
    public async Task PostPullRequestCommentAsync(
        StartCodeReviewCommand command,
        string body,
        CancellationToken cancellationToken = default)
    {
        var parts = command.RepoFullName.Split('/');
        var owner = parts[0];
        var repo  = parts[1];
        // GitHub PR 的一般評論使用 Issues API（/issues/ 而非 /pulls/）
        var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{command.PullRequestNumber}/comments";

        var payload = new { body = body };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GitHub PostPullRequestComment failed: {Status} - {Error}", response.StatusCode, error);
        }
    }

    // ────────────────────────────────────────────────
    // Response model（僅供內部反序列化）
    // ────────────────────────────────────────────────

    private sealed class GitHubPrFile
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("patch")]
        public string? Patch { get; set; }
    }
}
