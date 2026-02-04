namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// Dify API 使用紀錄與費用追蹤
/// </summary>
public class DifyUsageLog
{
    public Guid Id { get; set; }

    public Guid? ReviewLogId { get; set; }

    public Guid RuleId { get; set; }

    /// <summary>
    /// 請求 ID（Dify 回傳）
    /// </summary>
    public string? DifyRequestId { get; set; }

    /// <summary>
    /// 輸入 Token 數
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// 輸出 Token 數
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// 總 Token 數
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 預估費用（USD）
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// 使用的模型名稱
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// 請求耗時（毫秒）
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ReviewLog? ReviewLog { get; set; }
    public virtual Rule Rule { get; set; } = null!;
}
