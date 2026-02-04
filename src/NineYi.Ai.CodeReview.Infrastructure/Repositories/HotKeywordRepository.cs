using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class HotKeywordRepository : IHotKeywordRepository
{
    private readonly CodeReviewDbContext _context;

    public HotKeywordRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<HotKeyword?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.HotKeywords.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<HotKeyword>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.HotKeywords
            .Where(k => k.IsActive)
            .OrderBy(k => k.Severity)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<HotKeyword>> GetByCategoryAsync(KeywordCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.HotKeywords
            .Where(k => k.IsActive && k.Category == category)
            .OrderBy(k => k.Severity)
            .ToListAsync(cancellationToken);
    }

    public async Task<HotKeyword> AddAsync(HotKeyword keyword, CancellationToken cancellationToken = default)
    {
        _context.HotKeywords.Add(keyword);
        await _context.SaveChangesAsync(cancellationToken);
        return keyword;
    }

    public async Task UpdateAsync(HotKeyword keyword, CancellationToken cancellationToken = default)
    {
        keyword.UpdatedAt = DateTime.UtcNow;
        _context.HotKeywords.Update(keyword);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var keyword = await _context.HotKeywords.FindAsync(new object[] { id }, cancellationToken);
        if (keyword != null)
        {
            _context.HotKeywords.Remove(keyword);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementTriggerCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.HotKeywords
            .Where(k => k.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.TriggerCount, k => k.TriggerCount + 1), cancellationToken);
    }
}
