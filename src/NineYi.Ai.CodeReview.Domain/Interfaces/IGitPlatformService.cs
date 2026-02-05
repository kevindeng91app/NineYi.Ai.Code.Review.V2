using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

/// <summary>
/// Git 平台服務介面（GitHub, GitLab, Bitbucket）
/// </summary>
public interface IGitPlatformService
{
    GitPlatformType Platform { get; }

    Task<RepositoryInfo?> GetRepositoryInfoAsync(string repositoryFullName, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default);

    Task<PullRequestInfo> GetPullRequestAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<PullRequestFile>> GetPullRequestFilesAsync(string repositoryFullName, int pullRequestNumber, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default);

    Task<string> GetFileDiffAsync(string repositoryFullName, int pullRequestNumber, string filePath, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default);

    Task PostReviewCommentAsync(string repositoryFullName, int pullRequestNumber, string filePath, int line, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default);

    Task PostPullRequestCommentAsync(string repositoryFullName, int pullRequestNumber, string comment, string accessToken, string? apiBaseUrl = null, CancellationToken cancellationToken = default);

    bool ValidateWebhookSignature(string payload, string signature, string secret);
}

public class RepositoryInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultBranch { get; set; }
    public bool Private { get; set; }
}

public class PullRequestInfo
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string HeadCommitSha { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PullRequestFile
{
    public string FileName { get; set; } = string.Empty;
    public FileChangeType ChangeType { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public string? Patch { get; set; }
    public string? BlobUrl { get; set; }
}
