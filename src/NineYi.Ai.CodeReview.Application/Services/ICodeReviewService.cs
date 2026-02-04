using NineYi.Ai.CodeReview.Application.DTOs;

namespace NineYi.Ai.CodeReview.Application.Services;

public interface ICodeReviewService
{
    Task<ReviewResultDto> ProcessPullRequestAsync(WebhookPayload payload, CancellationToken cancellationToken = default);
    Task<ReviewResultDto?> GetReviewStatusAsync(Guid reviewLogId, CancellationToken cancellationToken = default);
}
