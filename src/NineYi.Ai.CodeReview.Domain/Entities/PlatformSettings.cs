namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 各 Git 平台的全域設定
/// </summary>
public class PlatformSettings
{
    public Guid Id { get; set; }

    /// <summary>
    /// Git 平台類型: GitHub, GitLab, Bitbucket
    /// </summary>
    public GitPlatformType Platform { get; set; }

    /// <summary>
    /// API 存取 Token
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
