namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// Repository 與 Rule 的對應關係
/// </summary>
public class RepositoryRuleMapping
{
    public Guid Id { get; set; }

    public Guid RepositoryId { get; set; }

    public Guid RuleId { get; set; }

    /// <summary>
    /// 此 mapping 的優先順序覆寫（null 則使用 Rule 的預設優先順序）
    /// </summary>
    public int? PriorityOverride { get; set; }

    /// <summary>
    /// 此 mapping 的檔案模式覆寫
    /// </summary>
    public string? FilePatternsOverride { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Repository Repository { get; set; } = null!;
    public virtual Rule Rule { get; set; } = null!;
}
