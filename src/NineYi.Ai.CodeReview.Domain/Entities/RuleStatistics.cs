namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 規則觸發統計（用於分析 Prompt 是否精準）
/// </summary>
public class RuleStatistics
{
    public Guid Id { get; set; }

    public Guid RuleId { get; set; }

    /// <summary>
    /// 統計日期
    /// </summary>
    public DateOnly StatDate { get; set; }

    /// <summary>
    /// 觸發次數
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// 產生評論的次數
    /// </summary>
    public int CommentGeneratedCount { get; set; }

    /// <summary>
    /// 無問題通過的次數
    /// </summary>
    public int PassedCount { get; set; }

    /// <summary>
    /// 消耗的 Token 總數
    /// </summary>
    public long TotalTokensConsumed { get; set; }

    /// <summary>
    /// 平均每次觸發的 Token 數
    /// </summary>
    public double AverageTokensPerTrigger { get; set; }

    // Navigation properties
    public virtual Rule Rule { get; set; } = null!;
}
