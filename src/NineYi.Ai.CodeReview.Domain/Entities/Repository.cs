namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 存放 Repository 設定（簡化版，認證資訊由 PlatformSettings 提供）
/// </summary>
public class Repository
{
    public Guid Id { get; set; }

    /// <summary>
    /// Repository 名稱（用於顯示）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Repository 完整名稱 (owner/repo)
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Git 平台類型: GitHub, GitLab, Bitbucket
    /// </summary>
    public GitPlatformType Platform { get; set; }

    /// <summary>
    /// 平台上的 Repository ID（由 API 自動取得）
    /// </summary>
    public string PlatformRepositoryId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<RepositoryRuleMapping> RuleMappings { get; set; } = new List<RepositoryRuleMapping>();
    public virtual ICollection<ReviewLog> ReviewLogs { get; set; } = new List<ReviewLog>();
}

public enum GitPlatformType
{
    GitHub = 1,
    GitLab = 2,
    Bitbucket = 3
}
