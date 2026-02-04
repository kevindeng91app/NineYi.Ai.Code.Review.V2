namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 存放 Repository 設定（包含各平台的存取資訊）
/// </summary>
public class Repository
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Git 平台類型: GitHub, GitLab, Bitbucket
    /// </summary>
    public GitPlatformType Platform { get; set; }

    /// <summary>
    /// 平台上的 Repository ID
    /// </summary>
    public string PlatformRepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// API 存取 Token（加密存儲）
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Webhook Secret（用於驗證 webhook 請求）
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// 平台 API 基礎 URL（GitLab 自架站用）
    /// </summary>
    public string? ApiBaseUrl { get; set; }

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
