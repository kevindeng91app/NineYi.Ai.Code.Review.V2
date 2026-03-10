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
/// GitLab REST API client，實作 <see cref="IRepoHostClient"/>。
/// 取 MR diff  ：GET /api/v4/projects/{projectId}/merge_requests/{iid}/diffs
/// 取 raw content：GET /api/v4/projects/{projectId}/repository/files/{encodedPath}/raw?ref={sha}
/// Token 透過 PRIVATE-TOKEN header 傳遞（不放 query string，避免出現在 log）。
/// </summary>
public class GitLabClient : IRepoHostClient
{
    private readonly HttpClient _http;
    private readonly string _accessToken;
    private readonly string _apiBaseUrl;
    private readonly FileExcludeOptions _excludeOptions;
    private readonly ILogger<GitLabClient> _logger;

    public GitLabClient(
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookSecretsOptions> secrets,
        IOptions<FileExcludeOptions> excludeOptions,
        ILogger<GitLabClient> logger)
    {
        _http = httpClientFactory.CreateClient("GitLab");
        _accessToken = secrets.Value.GitLab.AccessToken ?? string.Empty;
        // 移除尾端斜線，避免組 URL 時重複
        _apiBaseUrl = (secrets.Value.GitLab.ApiBaseUrl ?? string.Empty).TrimEnd('/');
        _excludeOptions = excludeOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileDiffItem>> GetPullRequestDiffFilesAsync(
        StartCodeReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var projectId = command.PlatformProjectId
            ?? throw new InvalidOperationException("GitLabClient requires PlatformProjectId on the command.");

        var url = $"{_apiBaseUrl}/api/v4/projects/{projectId}/merge_requests/{command.PullRequestNumber}/diffs";

        using var request = BuildRequest(HttpMethod.Get, url);
        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var diffs = await response.Content.ReadFromJsonAsync<List<GitLabDiffEntry>>(cancellationToken: cancellationToken)
                    ?? [];

        var result = new List<FileDiffItem>();

        foreach (var entry in diffs)
        {
            // 跳過純刪除的檔案（new_path 為空）或無 diff 內容
            if (string.IsNullOrEmpty(entry.NewPath) || string.IsNullOrEmpty(entry.Diff))
                continue;

            var diffItem = new FileDiffItem
            {
                FilePath = entry.NewPath,
                FileExtension = Path.GetExtension(entry.NewPath),
                Diff = entry.Diff,
                ChangedLineRanges = DiffHunkParser.Parse(entry.Diff)
            };

            if (!IsExcluded(diffItem))
                result.Add(diffItem);
        }

        _logger.LogInformation(
            "GitLab: MR !{Number} in project {ProjectId} — {Total} files fetched, {Kept} after filtering",
            command.PullRequestNumber, projectId, diffs.Count, result.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetFileRawContentAsync(
        StartCodeReviewCommand command,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var projectId = command.PlatformProjectId
            ?? throw new InvalidOperationException("GitLabClient requires PlatformProjectId on the command.");

        // GitLab 要求路徑中的 / 必須編碼為 %2F，否則回 404
        var encodedPath = Uri.EscapeDataString(filePath);

        var url = $"{_apiBaseUrl}/api/v4/projects/{projectId}/repository/files/{encodedPath}/raw" +
                  $"?ref={command.PullRequestRef.HeadCommitSha}";

        using var request = BuildRequest(HttpMethod.Get, url);
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
        // Token 放 Header，不放 query string（避免寫進 log）
        request.Headers.Add("PRIVATE-TOKEN", _accessToken);
        return request;
    }

    private bool IsExcluded(FileDiffItem item)
    {
        if (_excludeOptions.ExcludedPathPrefixes.Any(prefix =>
                item.FilePath.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (_excludeOptions.ExcludedFileExtensions.Any(ext =>
                item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    // ────────────────────────────────────────────────
    // Response model（僅供內部反序列化）
    // ────────────────────────────────────────────────

    private sealed class GitLabDiffEntry
    {
        [JsonPropertyName("new_path")]
        public string NewPath { get; set; } = string.Empty;

        [JsonPropertyName("diff")]
        public string? Diff { get; set; }
    }

    // TODO Phase 3 Step 6：實作 PostReviewCommentAsync
    public async Task PostReviewCommentAsync(
        StartCodeReviewCommand command,
        string filePath,
        int startLine,
        int endLine,
        string body,
        CancellationToken cancellationToken = default)
    {
        var projectId = command.PlatformProjectId;
        var iid       = command.PullRequestNumber;
        var sha       = command.PullRequestRef.HeadCommitSha;
        var url       = $"{_apiBaseUrl}/api/v4/projects/{projectId}/merge_requests/{iid}/discussions";

        var payload = new
        {
            body     = body,
            position = new
            {
                position_type = "text",
                base_sha      = sha,
                start_sha     = sha,
                head_sha      = sha,
                new_path      = filePath,
                new_line      = startLine
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("PRIVATE-TOKEN", _accessToken);
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GitLab PostReviewComment failed: {Status} - {Error}", response.StatusCode, error);
        }
    }

    // TODO Phase 3 Step 6：實作 PostPullRequestCommentAsync
    public async Task PostPullRequestCommentAsync(
        StartCodeReviewCommand command,
        string body,
        CancellationToken cancellationToken = default)
    {
        var projectId = command.PlatformProjectId;
        var iid       = command.PullRequestNumber;
        var url       = $"{_apiBaseUrl}/api/v4/projects/{projectId}/merge_requests/{iid}/notes";

        var payload = new { body = body };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("PRIVATE-TOKEN", _accessToken);
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GitLab PostPullRequestComment failed: {Status} - {Error}", response.StatusCode, error);
        }
    }
}
