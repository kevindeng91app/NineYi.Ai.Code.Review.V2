using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.DTOs;

namespace NineYi.Ai.CodeReview.Application.Services;

public interface ICodeReviewService
{
    /// <summary>
    /// Phase 1 統一入口：Controller 組裝 StartCodeReviewCommand 後呼叫此方法。
    /// 內含 PR Title Ignore Gate；未命中則橋接呼叫 ProcessPullRequestAsync。
    /// </summary>
    Task<ReviewResultDto> StartAsync(StartCodeReviewCommand command, CancellationToken cancellationToken = default);

    /// <summary>保留供橋接使用，Phase 2 移除。</summary>
    Task<ReviewResultDto> ProcessPullRequestAsync(WebhookPayload payload, CancellationToken cancellationToken = default);

    Task<ReviewResultDto?> GetReviewStatusAsync(Guid reviewLogId, CancellationToken cancellationToken = default);
}
