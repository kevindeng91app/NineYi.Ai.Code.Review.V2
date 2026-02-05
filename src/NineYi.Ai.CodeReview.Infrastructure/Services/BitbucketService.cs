using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Infrastructure.Services;

public class BitbucketService : IGitPlatformService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitbucketService> _logger;
    private const string DefaultApiBaseUrl = "https://api.bitbucket.org/2.0";

    public GitPlatformType Platform => GitPlatformType.Bitbucket;

    public BitbucketService(HttpClient httpClient, ILogger<BitbucketService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private void SetupHeaders(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<RepositoryInfo?> GetRepositoryInfoAsync(string repositoryFullName, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repositories/{repositoryFullName}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get repository info for {RepositoryFullName}: {StatusCode}", repositoryFullName, response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var repo = JsonSerializer.Deserialize<BitbucketRepository>(content, JsonOptions);

        return new RepositoryInfo
        {
            Id = repo!.Uuid ?? string.Empty,
            Name = repo.Name ?? string.Empty,
            FullName = repo.FullName ?? repositoryFullName,
            Description = repo.Description,
            DefaultBranch = repo.Mainbranch?.Name,
            Private = repo.IsPrivate
        };
    }

    public async Task<PullRequestInfo> GetPullRequestAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repositories/{repositoryFullName}/pullrequests/{pullRequestNumber}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var pr = JsonSerializer.Deserialize<BitbucketPullRequest>(content, JsonOptions);

        return new PullRequestInfo
        {
            Number = pr!.Id,
            Title = pr.Title ?? string.Empty,
            Description = pr.Description ?? string.Empty,
            Author = pr.Author?.DisplayName ?? pr.Author?.Nickname ?? string.Empty,
            SourceBranch = pr.Source?.Branch?.Name ?? string.Empty,
            TargetBranch = pr.Destination?.Branch?.Name ?? string.Empty,
            State = pr.State ?? string.Empty,
            HeadCommitSha = pr.Source?.Commit?.Hash ?? string.Empty,
            CreatedAt = pr.CreatedOn,
            UpdatedAt = pr.UpdatedOn
        };
    }

    public async Task<IEnumerable<PullRequestFile>> GetPullRequestFilesAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repositories/{repositoryFullName}/pullrequests/{pullRequestNumber}/diffstat";

        var allFiles = new List<BitbucketDiffStat>();
        var nextUrl = url;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var response = await _httpClient.GetAsync(nextUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<BitbucketPagedResult<BitbucketDiffStat>>(content, JsonOptions);

            if (result?.Values != null)
            {
                allFiles.AddRange(result.Values);
            }

            nextUrl = result?.Next;
        }

        // 獲取每個檔案的 diff
        var filesWithDiff = new List<PullRequestFile>();
        foreach (var file in allFiles)
        {
            var diff = await GetFileDiffAsync(repositoryFullName, pullRequestNumber, file.New?.Path ?? file.Old?.Path ?? string.Empty, accessToken, apiBaseUrl, cancellationToken);
            filesWithDiff.Add(new PullRequestFile
            {
                FileName = file.New?.Path ?? file.Old?.Path ?? string.Empty,
                ChangeType = ParseChangeType(file.Status),
                Additions = file.LinesAdded,
                Deletions = file.LinesRemoved,
                Patch = diff
            });
        }

        return filesWithDiff;
    }

    public async Task<string> GetFileDiffAsync(string repositoryFullName, int pullRequestNumber, string filePath, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repositories/{repositoryFullName}/pullrequests/{pullRequestNumber}/diff";

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var fullDiff = await response.Content.ReadAsStringAsync(cancellationToken);

        // 從完整 diff 中提取指定檔案的部分
        return ExtractFileDiff(fullDiff, filePath);
    }

    private static string ExtractFileDiff(string fullDiff, string filePath)
    {
        var lines = fullDiff.Split('\n');
        var result = new StringBuilder();
        var inFile = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("diff --git"))
            {
                inFile = line.Contains($"b/{filePath}");
                if (inFile)
                {
                    result.AppendLine(line);
                }
            }
            else if (inFile)
            {
                if (line.StartsWith("diff --git"))
                {
                    break;
                }
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }

    public async Task PostReviewCommentAsync(string repositoryFullName, int pullRequestNumber, string filePath, int line, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repositories/{repositoryFullName}/pullrequests/{pullRequestNumber}/comments";

        var payload = new
        {
            content = new { raw = comment },
            inline = new
            {
                path = filePath,
                to = line
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
            _logger.LogWarning(ex, "Failed to post inline comment on line {Line}, trying PR comment instead", line);
            await PostPullRequestCommentAsync(repositoryFullName, pullRequestNumber,
                $"**{filePath}** (line {line}):\n\n{comment}", accessToken, apiBaseUrl, cancellationToken);
        }
    }

    public async Task PostPullRequestCommentAsync(string repositoryFullName, int pullRequestNumber, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default)
    {
        SetupHeaders(accessToken);
        var baseUrl = apiBaseUrl ?? DefaultApiBaseUrl;
        var url = $"{baseUrl}/repositories/{repositoryFullName}/pullrequests/{pullRequestNumber}/comments";

        var payload = new
        {
            content = new { raw = comment }
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        // Bitbucket Cloud 沒有 webhook signature，Server 版本才有
        // 這裡簡單返回 true，實際應用中應該考慮其他驗證方式
        return true;
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

    private class BitbucketPullRequest
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? State { get; set; }
        public BitbucketBranchRef? Source { get; set; }
        public BitbucketBranchRef? Destination { get; set; }
        public BitbucketUser? Author { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    private class BitbucketBranchRef
    {
        public BitbucketBranch? Branch { get; set; }
        public BitbucketCommit? Commit { get; set; }
    }

    private class BitbucketBranch
    {
        public string? Name { get; set; }
    }

    private class BitbucketCommit
    {
        public string? Hash { get; set; }
    }

    private class BitbucketUser
    {
        public string? DisplayName { get; set; }
        public string? Nickname { get; set; }
    }

    private class BitbucketDiffStat
    {
        public string? Status { get; set; }
        public BitbucketFilePath? Old { get; set; }
        public BitbucketFilePath? New { get; set; }
        public int LinesAdded { get; set; }
        public int LinesRemoved { get; set; }
    }

    private class BitbucketFilePath
    {
        public string? Path { get; set; }
    }

    private class BitbucketPagedResult<T>
    {
        public List<T>? Values { get; set; }
        public string? Next { get; set; }
    }

    private class BitbucketRepository
    {
        public string? Uuid { get; set; }
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public BitbucketMainbranch? Mainbranch { get; set; }
    }

    private class BitbucketMainbranch
    {
        public string? Name { get; set; }
    }
}
