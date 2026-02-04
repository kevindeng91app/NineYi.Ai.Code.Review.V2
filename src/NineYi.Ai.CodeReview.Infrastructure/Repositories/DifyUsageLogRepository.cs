using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class DifyUsageLogRepository : IDifyUsageLogRepository
{
    private readonly CodeReviewDbContext _context;

    public DifyUsageLogRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<DifyUsageLog> AddAsync(DifyUsageLog usageLog, CancellationToken cancellationToken = default)
    {
        _context.DifyUsageLogs.Add(usageLog);
        await _context.SaveChangesAsync(cancellationToken);
        return usageLog;
    }

    public async Task<IEnumerable<DifyUsageLog>> GetByReviewLogIdAsync(Guid reviewLogId, CancellationToken cancellationToken = default)
    {
        return await _context.DifyUsageLogs
            .Where(l => l.ReviewLogId == reviewLogId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalCostAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
        var toDateTime = toDate.ToDateTime(TimeOnly.MaxValue);

        return await _context.DifyUsageLogs
            .Where(l => l.CreatedAt >= fromDateTime && l.CreatedAt <= toDateTime && l.IsSuccess)
            .SumAsync(l => l.EstimatedCost, cancellationToken);
    }

    public async Task<(int TotalTokens, decimal TotalCost)> GetUsageSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
        var toDateTime = toDate.ToDateTime(TimeOnly.MaxValue);

        var result = await _context.DifyUsageLogs
            .Where(l => l.CreatedAt >= fromDateTime && l.CreatedAt <= toDateTime && l.IsSuccess)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalTokens = g.Sum(l => l.TotalTokens),
                TotalCost = g.Sum(l => l.EstimatedCost)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result != null ? (result.TotalTokens, result.TotalCost) : (0, 0);
    }

    public async Task<IEnumerable<(Guid RuleId, string RuleName, decimal Cost, int Tokens)>> GetCostByRuleAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
        var toDateTime = toDate.ToDateTime(TimeOnly.MaxValue);

        var result = await _context.DifyUsageLogs
            .Where(l => l.CreatedAt >= fromDateTime && l.CreatedAt <= toDateTime && l.IsSuccess)
            .Include(l => l.Rule)
            .GroupBy(l => new { l.RuleId, l.Rule.Name })
            .Select(g => new
            {
                g.Key.RuleId,
                RuleName = g.Key.Name,
                Cost = g.Sum(l => l.EstimatedCost),
                Tokens = g.Sum(l => l.TotalTokens)
            })
            .OrderByDescending(r => r.Cost)
            .ToListAsync(cancellationToken);

        return result.Select(r => (r.RuleId, r.RuleName, r.Cost, r.Tokens));
    }
}
