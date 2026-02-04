namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 存放 Dify API 規則
/// </summary>
public class Rule
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Dify API Endpoint
    /// </summary>
    public string DifyApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Dify API Key
    /// </summary>
    public string DifyApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 規則類型：Security, CodeStyle, Performance, BestPractice 等
    /// </summary>
    public RuleType Type { get; set; }

    /// <summary>
    /// 規則優先順序（數字越小優先順序越高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 適用的檔案模式（如 *.cs, *.js）
    /// </summary>
    public string? FilePatterns { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<RepositoryRuleMapping> RepositoryMappings { get; set; } = new List<RepositoryRuleMapping>();
    public virtual ICollection<RuleStatistics> Statistics { get; set; } = new List<RuleStatistics>();
}

public enum RuleType
{
    Security = 1,
    CodeStyle = 2,
    Performance = 3,
    BestPractice = 4,
    Documentation = 5,
    Testing = 6,
    Custom = 99
}
