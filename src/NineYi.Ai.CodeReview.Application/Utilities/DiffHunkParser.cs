using System.Text.RegularExpressions;
using NineYi.Ai.CodeReview.Application.Models;

namespace NineYi.Ai.CodeReview.Application.Utilities;

/// <summary>
/// 解析 unified diff 中的 hunk header，提取 new 端行號範圍。
/// </summary>
public static class DiffHunkParser
{
    // 匹配 @@ -old[,count] +newStart[,newCount] @@ 格式
    // 例如：@@ -10,5 +12,8 @@  或  @@ -0,0 +1 @@
    private static readonly Regex HunkHeaderRegex = new(
        @"@@\s+-\d+(?:,\d+)?\s+\+(\d+)(?:,(\d+))?\s+@@",
        RegexOptions.Compiled);

    /// <summary>
    /// 解析 diff 文字，回傳每個 hunk 對應的 new 端行號範圍。
    /// </summary>
    /// <param name="diff">unified diff 文字（單一檔案或多 hunk）。</param>
    /// <returns>
    /// 每個 hunk 一個 <see cref="LineRange"/>（Start, End 皆為 1-based, inclusive）。
    /// newCount=0 的 hunk（純刪除，no added lines）會被略過。
    /// </returns>
    public static IReadOnlyList<LineRange> Parse(string diff)
    {
        if (string.IsNullOrEmpty(diff))
            return [];

        var ranges = new List<LineRange>();

        foreach (Match match in HunkHeaderRegex.Matches(diff))
        {
            var newStart = int.Parse(match.Groups[1].Value);

            // newCount 省略時代表只有 1 行；newCount=0 代表純刪除，略過
            var newCount = match.Groups[2].Success
                ? int.Parse(match.Groups[2].Value)
                : 1;

            if (newCount == 0)
                continue;

            ranges.Add(new LineRange(newStart, newStart + newCount - 1));
        }

        return ranges;
    }
}
