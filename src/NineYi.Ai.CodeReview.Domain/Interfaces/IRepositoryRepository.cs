using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IRepositoryRepository
{
    Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Repository?> GetByPlatformIdAsync(GitPlatformType platform, string platformRepositoryId, CancellationToken cancellationToken = default);
    Task<Repository?> GetByFullNameAsync(GitPlatformType platform, string fullName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Repository>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default);
    Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
