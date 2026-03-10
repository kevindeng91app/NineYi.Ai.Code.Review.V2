namespace NineYi.Ai.CodeReview.Application.Commands;

/// <summary>
/// 指向 PR/MR 資料來源的參考資訊。
/// 打包「如何取得這個 PR 的內容」，讓 Service 層在 Phase 2 取 diff / raw content 時有明確的依據。
/// </summary>
public class PullRequestRef
{
    /// <summary>
    /// Source/Head branch 的 commit SHA。
    /// Phase 2 抓 raw file content 時使用（確保取到 PR 分支的版本）
    /// </summary>
    public string HeadCommitSha { get; set; } = string.Empty;

    /// <summary>來源分支名稱（PR 的 feature branch）</summary>
    public string SourceBranch { get; set; } = string.Empty;

    /// <summary>目標分支名稱（PR 要合入的 branch，如 main / develop）</summary>
    public string TargetBranch { get; set; } = string.Empty;

    /// <summary>
    /// 直接可用的 Diff URL。
    /// - GitHub：$.pull_request.diff_url（無需額外 API call）
    /// - Bitbucket：$.pullrequest.links.diff.href（需 Bearer Token）
    /// - GitLab：null（GitLab webhook payload 不提供，Phase 2 由 GitLabClient 自行組裝）
    /// </summary>
    public string? DiffUrl { get; set; }
}
