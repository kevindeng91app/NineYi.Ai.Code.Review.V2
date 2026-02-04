using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IRuleRepository
{
    Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Rule>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Rule>> GetByRepositoryIdAsync(Guid repositoryId, CancellationToken cancellationToken = default);
    Task<Rule> AddAsync(Rule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
