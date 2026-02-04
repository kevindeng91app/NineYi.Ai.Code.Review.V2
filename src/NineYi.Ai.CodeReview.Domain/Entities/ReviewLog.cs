namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// Code Review 執行紀錄
/// </summary>
public class ReviewLog
{
    public Guid Id { get; set; }

    public Guid RepositoryId { get; set; }

    /// <summary>
    /// Pull Request 編號
    /// </summary>
    public int PullRequestNumber { get; set; }

    /// <summary>
    /// Pull Request 標題
    /// </summary>
    public string PullRequestTitle { get; set; } = string.Empty;

    /// <summary>
    /// PR 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 執行狀態
    /// </summary>
    public ReviewStatus Status { get; set; }

    /// <summary>
    /// 開始時間
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// 完成時間
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 處理的檔案數量
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// 產生的評論數量
    /// </summary>
    public int CommentsGenerated { get; set; }

    /// <summary>
    /// Dify API 消耗的 Token 數
    /// </summary>
    public int TokensConsumed { get; set; }

    /// <summary>
    /// 預估花費（USD）
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// 錯誤訊息（如果有的話）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 詳細執行日誌（JSON 格式）
    /// </summary>
    public string? DetailedLog { get; set; }

    // Navigation properties
    public virtual Repository Repository { get; set; } = null!;
    public virtual ICollection<ReviewFileLog> FileLogs { get; set; } = new List<ReviewFileLog>();
}

public enum ReviewStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4
}
