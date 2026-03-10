using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.Models;

namespace NineYi.Ai.CodeReview.Application.Abstractions;

/// <summary>
/// 平台無關的 Git 代管平台 client 介面。
/// GitHub / GitLab / Bitbucket 各自實作，對外提供相同的操作介面。
/// </summary>
public interface IRepoHostClient
{
    /// <summary>
    /// 取得 PR/MR 中所有異動檔案的 diff 清單。
    /// 回傳前已依 <see cref="Options.FileExcludeOptions"/> 過濾排除路徑與副檔名。
    /// </summary>
    Task<IReadOnlyList<FileDiffItem>> GetPullRequestDiffFilesAsync(
        StartCodeReviewCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得指定檔案在 PR head commit 的原始內容（raw text）。
    /// </summary>
    Task<string> GetFileRawContentAsync(
        StartCodeReviewCommand command,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在 PR/MR 的指定行號範圍張貼 inline code review comment。
    /// startLine / endLine 為 new file 的行號（1-based）。
    /// 若平台不支援行號範圍，以 startLine 為準。
    /// </summary>
    /// <param name="command">webhook 解析後的命令物件。</param>
    /// <param name="filePath">檔案完整路徑，例如 "src/Services/FooService.cs"。</param>
    /// <param name="startLine">評論起始行號（1-based）。</param>
    /// <param name="endLine">評論結束行號（1-based）。</param>
    /// <param name="body">評論內容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task PostReviewCommentAsync(
        StartCodeReviewCommand command,
        string filePath,
        int startLine,
        int endLine,
        string body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在 PR/MR 層級張貼一般 comment（不指定檔案或行號）。
    /// 用於「沒有發現問題」的整體通知，或行號 parse 失敗時的 fallback。
    /// </summary>
    /// <param name="command">webhook 解析後的命令物件。</param>
    /// <param name="body">評論內容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task PostPullRequestCommentAsync(
        StartCodeReviewCommand command,
        string body,
        CancellationToken cancellationToken = default);
}
