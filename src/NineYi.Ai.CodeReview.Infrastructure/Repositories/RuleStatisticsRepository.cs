using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class RuleStatisticsRepository : IRuleStatisticsRepository
{
    private readonly CodeReviewDbContext _context;

    public RuleStatisticsRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<RuleStatistics?> GetByRuleAndDateAsync(Guid ruleId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.RuleStatistics
            .FirstOrDefaultAsync(s => s.RuleId == ruleId && s.StatDate == date, cancellationToken);
    }

    public async Task<IEnumerable<RuleStatistics>> GetByRuleIdAsync(Guid ruleId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        return await _context.RuleStatistics
            .Where(s => s.RuleId == ruleId && s.StatDate >= fromDate && s.StatDate <= toDate)
            .OrderBy(s => s.StatDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RuleStatistics>> GetTopTriggeredRulesAsync(DateOnly fromDate, DateOnly toDate, int top = 10, CancellationToken cancellationToken = default)
    {
        return await _context.RuleStatistics
            .Where(s => s.StatDate >= fromDate && s.StatDate <= toDate)
            .GroupBy(s => s.RuleId)
            .Select(g => new RuleStatistics
            {
                RuleId = g.Key,
                TriggerCount = g.Sum(s => s.TriggerCount),
                CommentGeneratedCount = g.Sum(s => s.CommentGeneratedCount),
                PassedCount = g.Sum(s => s.PassedCount),
                TotalTokensConsumed = g.Sum(s => s.TotalTokensConsumed),
                AverageTokensPerTrigger = g.Sum(s => s.TriggerCount) > 0
                    ? (double)g.Sum(s => s.TotalTokensConsumed) / g.Sum(s => s.TriggerCount)
                    : 0
            })
            .OrderByDescending(s => s.TriggerCount)
            .Take(top)
            .ToListAsync(cancellationToken);
    }

    public async Task<RuleStatistics> AddOrUpdateAsync(RuleStatistics statistics, CancellationToken cancellationToken = default)
    {
        var existing = await GetByRuleAndDateAsync(statistics.RuleId, statistics.StatDate, cancellationToken);
        if (existing != null)
        {
            existing.TriggerCount = statistics.TriggerCount;
            existing.CommentGeneratedCount = statistics.CommentGeneratedCount;
            existing.PassedCount = statistics.PassedCount;
            existing.TotalTokensConsumed = statistics.TotalTokensConsumed;
            existing.AverageTokensPerTrigger = statistics.AverageTokensPerTrigger;
            _context.RuleStatistics.Update(existing);
        }
        else
        {
            statistics.Id = Guid.NewGuid();
            _context.RuleStatistics.Add(statistics);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? statistics;
    }

    public async Task IncrementTriggerAsync(Guid ruleId, DateOnly date, int tokensConsumed, bool hasComment, CancellationToken cancellationToken = default)
    {
        var existing = await GetByRuleAndDateAsync(ruleId, date, cancellationToken);
        if (existing != null)
        {
            existing.TriggerCount++;
            existing.TotalTokensConsumed += tokensConsumed;
            if (hasComment)
                existing.CommentGeneratedCount++;
            else
                existing.PassedCount++;
            existing.AverageTokensPerTrigger = (double)existing.TotalTokensConsumed / existing.TriggerCount;
            _context.RuleStatistics.Update(existing);
        }
        else
        {
            var statistics = new RuleStatistics
            {
                Id = Guid.NewGuid(),
                RuleId = ruleId,
                StatDate = date,
                TriggerCount = 1,
                TotalTokensConsumed = tokensConsumed,
                CommentGeneratedCount = hasComment ? 1 : 0,
                PassedCount = hasComment ? 0 : 1,
                AverageTokensPerTrigger = tokensConsumed
            };
            _context.RuleStatistics.Add(statistics);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
