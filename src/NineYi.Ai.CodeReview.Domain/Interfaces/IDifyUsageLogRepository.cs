using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IDifyUsageLogRepository
{
    Task<DifyUsageLog> AddAsync(DifyUsageLog usageLog, CancellationToken cancellationToken = default);
    Task<IEnumerable<DifyUsageLog>> GetByReviewLogIdAsync(Guid reviewLogId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCostAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<(int TotalTokens, decimal TotalCost)> GetUsageSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<(Guid RuleId, string RuleName, decimal Cost, int Tokens)>> GetCostByRuleAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
