using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IHotKeywordRepository
{
    Task<HotKeyword?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<HotKeyword>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<HotKeyword>> GetByCategoryAsync(KeywordCategory category, CancellationToken cancellationToken = default);
    Task<HotKeyword> AddAsync(HotKeyword keyword, CancellationToken cancellationToken = default);
    Task UpdateAsync(HotKeyword keyword, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task IncrementTriggerCountAsync(Guid id, CancellationToken cancellationToken = default);
}
