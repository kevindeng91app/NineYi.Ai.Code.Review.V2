namespace NineYi.Ai.CodeReview.Application.Models;

/// <summary>
/// 單一檔案的 diff 資訊，由 IRepoHostClient 取得並回傳。
/// Diff 文字保持原始格式，行號範圍獨立存放於 ChangedLineRanges，兩者互不污染。
/// </summary>
public class FileDiffItem
{
    /// <summary>
    /// 檔案完整路徑（含目錄），例如 "src/Services/FooService.cs"。
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 副檔名（含句點），例如 ".cs"、".ts"。
    /// 用於依 FileType 篩選適用的 Rules（例如只對 .cs 套用 C# 規則）。
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// 該檔案的完整 unified diff 文字（原始格式，未加工）。
    /// 可直接送給 Dify，不含任何行號標記。
    /// </summary>
    public string Diff { get; set; } = string.Empty;

    /// <summary>
    /// 從 diff hunk header 解析出的 new 端行號範圍集合。
    /// 每個 hunk（<c>@@ -old +new @@</c>）產生一個 <see cref="LineRange"/>。
    /// 例如 <c>@@ -10,5 +12,8 @@</c> → <c>LineRange(Start=12, End=19)</c>。
    /// </summary>
    public IReadOnlyList<LineRange> ChangedLineRanges { get; set; } = [];
}

/// <summary>
/// New 端行號範圍（inclusive，1-based）。
/// 對應 diff hunk header 中 <c>+newStart,newCount</c> 的範圍。
/// </summary>
/// <param name="Start">起始行號（含）。</param>
/// <param name="End">結束行號（含）。</param>
public record LineRange(int Start, int End);
