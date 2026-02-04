namespace NineYi.Ai.CodeReview.Domain.Entities;

/// <summary>
/// 關鍵字 Hottable - 用於判斷並提示 RD 注意特定關鍵字
/// </summary>
public class HotKeyword
{
    public Guid Id { get; set; }

    /// <summary>
    /// 關鍵字
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 關鍵字類別
    /// </summary>
    public KeywordCategory Category { get; set; }

    /// <summary>
    /// 嚴重程度
    /// </summary>
    public SeverityLevel Severity { get; set; }

    /// <summary>
    /// 當偵測到此關鍵字時要顯示的訊息
    /// </summary>
    public string AlertMessage { get; set; } = string.Empty;

    /// <summary>
    /// 是否使用正則表達式匹配
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    /// 適用的檔案模式（如 *.cs, *.config）
    /// </summary>
    public string? FilePatterns { get; set; }

    /// <summary>
    /// 觸發次數統計
    /// </summary>
    public int TriggerCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public enum KeywordCategory
{
    Security = 1,          // 安全性關鍵字（如 password, secret, token）
    Performance = 2,       // 效能關鍵字（如 Thread.Sleep, lock）
    Deprecated = 3,        // 過時 API 或方法
    Configuration = 4,     // 設定相關（如 connection string）
    Sensitive = 5,         // 敏感資料（如 個資欄位）
    Custom = 99
}

public enum SeverityLevel
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
