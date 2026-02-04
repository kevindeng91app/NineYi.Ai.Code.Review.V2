using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IRuleStatisticsRepository
{
    Task<RuleStatistics?> GetByRuleAndDateAsync(Guid ruleId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuleStatistics>> GetByRuleIdAsync(Guid ruleId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuleStatistics>> GetTopTriggeredRulesAsync(DateOnly fromDate, DateOnly toDate, int top = 10, CancellationToken cancellationToken = default);
    Task<RuleStatistics> AddOrUpdateAsync(RuleStatistics statistics, CancellationToken cancellationToken = default);
    Task IncrementTriggerAsync(Guid ruleId, DateOnly date, int tokensConsumed, bool hasComment, CancellationToken cancellationToken = default);
}
