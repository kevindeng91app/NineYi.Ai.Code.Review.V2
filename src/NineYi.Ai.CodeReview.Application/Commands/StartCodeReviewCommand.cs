using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Application.Commands;

/// <summary>
/// 啟動 Code Review 的統一入口命令。
/// Controller 將各平台 webhook payload 解析後組裝為此物件，傳入 ICodeReviewService.StartAsync()。
/// </summary>
public class StartCodeReviewCommand
{
    /// <summary>來源平台（GitHub / GitLab / Bitbucket）</summary>
    public GitPlatformType ProviderType { get; set; }

    /// <summary>
    /// Repository 全名，格式依平台而異：
    /// - GitHub：  "owner/repo"
    /// - GitLab：  "group/repo"（path_with_namespace）
    /// - Bitbucket："workspace/repo-slug"
    /// </summary>
    public string RepoFullName { get; set; } = string.Empty;

    /// <summary>PR / MR 編號（GitHub/Bitbucket 用 id，GitLab 用 iid）</summary>
    public int PullRequestNumber { get; set; }

    /// <summary>PR 標題，用於 PR Title Ignore Gate 判斷</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>指向 PR 資料來源的參考資訊（branch、commit sha、diff url）</summary>
    public PullRequestRef PullRequestRef { get; set; } = new();
}
