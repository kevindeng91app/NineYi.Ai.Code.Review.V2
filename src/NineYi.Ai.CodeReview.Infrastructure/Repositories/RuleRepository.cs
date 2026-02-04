using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class RuleRepository : IRuleRepository
{
    private readonly CodeReviewDbContext _context;

    public RuleRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rules.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Rule>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Rule>> GetByRepositoryIdAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        return await _context.RepositoryRuleMappings
            .Where(m => m.RepositoryId == repositoryId && m.IsActive)
            .Include(m => m.Rule)
            .Where(m => m.Rule.IsActive)
            .OrderBy(m => m.PriorityOverride ?? m.Rule.Priority)
            .Select(m => m.Rule)
            .ToListAsync(cancellationToken);
    }

    public async Task<Rule> AddAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.Rules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _context.Rules.FindAsync(new object[] { id }, cancellationToken);
        if (rule != null)
        {
            _context.Rules.Remove(rule);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
