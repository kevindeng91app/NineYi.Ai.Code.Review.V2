using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.DTOs;

namespace NineYi.Ai.CodeReview.Application.Services;

public interface ICodeReviewService
{
    /// <summary>
    /// Phase 2 統一入口：Controller 組裝 StartCodeReviewCommand 後呼叫此方法。
    /// 內含 PR Title Ignore Gate；透過 IRepoHostClientFactory 取得 diff 檔案清單。
    /// </summary>
    Task<ReviewResultDto> StartAsync(StartCodeReviewCommand command, CancellationToken cancellationToken = default);

}
