using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineYi.Ai.CodeReview.Application.Abstractions;
using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.Models;
using NineYi.Ai.CodeReview.Application.Options;
using NineYi.Ai.CodeReview.Application.Utilities;

namespace NineYi.Ai.CodeReview.Infrastructure.Clients;

/// <summary>
/// Bitbucket REST API client，實作 <see cref="IRepoHostClient"/>。
/// <para>
/// 取 PR diff：DiffUrl（webhook payload）→ 302 Redirect → CDN raw unified diff 文字。
/// 取 raw content：GET /2.0/repositories/{ws}/{repo}/src/{sha}/{path}（Basic Auth）。
/// </para>
/// </summary>
public class BitbucketClient : IRepoHostClient
{
    // Bitbucket diff 回傳的是 raw unified diff，多個檔案合在一起，用此 regex 切分
    private static readonly Regex DiffSplitRegex = new(
        @"diff --git[\s\S]*?(?=diff --git|$)",
        RegexOptions.Compiled);

    // 從 diff 段落取出 "+++ b/{path}" 的檔案路徑
    private static readonly Regex FilePathRegex = new(
        @"^\+\+\+ b/(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly string _accessToken;
    private readonly string _username;
    private readonly FileExcludeOptions _excludeOptions;
    private readonly ILogger<BitbucketClient> _logger;

    public BitbucketClient(
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookSecretsOptions> secrets,
        IOptions<FileExcludeOptions> excludeOptions,
        ILogger<BitbucketClient> logger)
    {
        _http = httpClientFactory.CreateClient("Bitbucket");
        _accessToken = secrets.Value.Bitbucket.AccessToken ?? string.Empty;
        _username = secrets.Value.Bitbucket.Username ?? string.Empty;
        _excludeOptions = excludeOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileDiffItem>> GetPullRequestDiffFilesAsync(
        StartCodeReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var diffUrl = command.PullRequestRef.DiffUrl
            ?? throw new InvalidOperationException("BitbucketClient requires DiffUrl on PullRequestRef.");

        var rawDiff = await FetchRawDiffAsync(diffUrl, cancellationToken);
        var result = ParseRawDiff(rawDiff);

        _logger.LogInformation(
            "Bitbucket: PR #{Number} in {Repo} — {Kept} files after filtering",
            command.PullRequestNumber, command.RepoFullName, result.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetFileRawContentAsync(
        StartCodeReviewCommand command,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        // RepoFullName 格式："{workspace}/{repo-slug}"
        var parts = command.RepoFullName.Split('/', 2);
        if (parts.Length != 2)
            throw new InvalidOperationException($"Cannot parse workspace/repo from RepoFullName: {command.RepoFullName}");

        var workspace = parts[0];
        var repo = parts[1];
        var url = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repo}/src" +
                  $"/{command.PullRequestRef.HeadCommitSha}/{filePath}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Bitbucket src API 用 Basic Auth（username + app password）
        request.Headers.Add("Authorization", BuildBasicAuth(_username, _accessToken));

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    // ────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────

    /// <summary>
    /// 打 Bitbucket DiffUrl，處理 302 redirect（第二步不帶 Auth）。
    /// </summary>
    private async Task<string> FetchRawDiffAsync(string diffUrl, CancellationToken cancellationToken)
    {
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, diffUrl);
        firstRequest.Headers.Add("Authorization", $"Bearer {_accessToken}");

        // AllowAutoRedirect = false 必須在 HttpClient 建立時設定（見 DI 註冊）
        var firstResponse = await _http.SendAsync(firstRequest, cancellationToken);

        if (firstResponse.StatusCode == HttpStatusCode.Redirect
            || firstResponse.StatusCode == HttpStatusCode.MovedPermanently)
        {
            var cdnUrl = firstResponse.Headers.Location
                ?? throw new InvalidOperationException("Bitbucket 302 response missing Location header.");

            // 第二步：打 CDN，明確不帶 Authorization
            using var cdnRequest = new HttpRequestMessage(HttpMethod.Get, cdnUrl);
            var cdnResponse = await _http.SendAsync(cdnRequest, cancellationToken);
            cdnResponse.EnsureSuccessStatusCode();
            return await cdnResponse.Content.ReadAsStringAsync(cancellationToken);
        }

        firstResponse.EnsureSuccessStatusCode();
        return await firstResponse.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// 將 raw unified diff 文字切分成多個 FileDiffItem。
    /// </summary>
    private List<FileDiffItem> ParseRawDiff(string rawDiff)
    {
        var result = new List<FileDiffItem>();

        foreach (Match match in DiffSplitRegex.Matches(rawDiff))
        {
            var section = match.Value;
            var pathMatch = FilePathRegex.Match(section);
            if (!pathMatch.Success)
                continue;

            var filePath = pathMatch.Groups[1].Value.Trim();

            var diffItem = new FileDiffItem
            {
                FilePath = filePath,
                FileExtension = Path.GetExtension(filePath),
                Diff = section,
                ChangedLineRanges = DiffHunkParser.Parse(section)
            };

            if (!IsExcluded(diffItem))
                result.Add(diffItem);
        }

        return result;
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

    private static string BuildBasicAuth(string username, string password)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return $"Basic {credentials}";
    }

    // TODO Phase 3 Step 7：實作 PostReviewCommentAsync
    public async Task PostReviewCommentAsync(
        StartCodeReviewCommand command,
        string filePath,
        int startLine,
        int endLine,
        string body,
        CancellationToken cancellationToken = default)
    {
        var parts     = command.RepoFullName.Split('/');
        var workspace = parts[0];
        var repoSlug  = parts[1];
        var url       = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{command.PullRequestNumber}/comments";

        var payload = new
        {
            content = new { raw = body },
            inline  = new
            {
                path = filePath,
                from = startLine,
                to   = endLine
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        request.Content = System.Net.Http.Json.JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Bitbucket PostReviewComment failed: {Status} - {Error}", response.StatusCode, error);
        }
    }

    // TODO Phase 3 Step 7：實作 PostPullRequestCommentAsync
    public async Task PostPullRequestCommentAsync(
        StartCodeReviewCommand command,
        string body,
        CancellationToken cancellationToken = default)
    {
        var parts     = command.RepoFullName.Split('/');
        var workspace = parts[0];
        var repoSlug  = parts[1];
        var url       = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{command.PullRequestNumber}/comments";

        var payload = new { content = new { raw = body } };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        request.Content = System.Net.Http.Json.JsonContent.Create(payload);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Bitbucket PostPullRequestComment failed: {Status} - {Error}", response.StatusCode, error);
        }
    }
}
