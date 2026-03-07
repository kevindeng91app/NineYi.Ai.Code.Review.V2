namespace NineYi.Ai.CodeReview.Application.Options;

/// <summary>
/// PR Title Ignore Gate 設定。
/// 符合關鍵字的 PR 將跳過 Code Review，不抓 diff、不呼叫 Dify，節省成本。
/// </summary>
public class PullRequestIgnoreOptions
{
    public const string SectionName = "PullRequestIgnore";

    /// <summary>
    /// PR 標題包含這些關鍵字時，跳過 Code Review。
    /// 比對方式：Contains（不分大小寫）
    /// 範例：["[WIP]", "[SKIP]", "[NO-REVIEW]", "auto-", "chore:"]
    /// </summary>
    public List<string> TitleKeywords { get; set; } = new();
}
