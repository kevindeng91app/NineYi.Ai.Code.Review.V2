using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IReviewLogRepository
{
    Task<ReviewLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReviewLog?> GetByPullRequestAsync(Guid repositoryId, int pullRequestNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReviewLog>> GetByRepositoryIdAsync(Guid repositoryId, int pageSize = 20, int page = 1, CancellationToken cancellationToken = default);
    Task<ReviewLog> AddAsync(ReviewLog reviewLog, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReviewLog reviewLog, CancellationToken cancellationToken = default);
    Task AddFileLogAsync(ReviewFileLog fileLog, CancellationToken cancellationToken = default);
}
