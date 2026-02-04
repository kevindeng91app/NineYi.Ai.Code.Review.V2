namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 單一檔案的 Review 紀錄
/// </summary>
public class ReviewFileLog
{
    public Guid Id { get; set; }

    public Guid ReviewLogId { get; set; }

    /// <summary>
    /// 檔案路徑
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 檔案狀態：Added, Modified, Deleted
    /// </summary>
    public FileChangeType ChangeType { get; set; }

    /// <summary>
    /// 新增行數
    /// </summary>
    public int LinesAdded { get; set; }

    /// <summary>
    /// 刪除行數
    /// </summary>
    public int LinesDeleted { get; set; }

    /// <summary>
    /// 是否有產生評論
    /// </summary>
    public bool HasComments { get; set; }

    /// <summary>
    /// 產生的評論內容（JSON 陣列）
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// 符合的關鍵字（來自 HotKeyword）
    /// </summary>
    public string? MatchedKeywords { get; set; }

    /// <summary>
    /// 處理此檔案消耗的 Token 數
    /// </summary>
    public int TokensConsumed { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ReviewLog ReviewLog { get; set; } = null!;
}

public enum FileChangeType
{
    Added = 1,
    Modified = 2,
    Deleted = 3,
    Renamed = 4
}
