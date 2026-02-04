using Microsoft.Extensions.Logging;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IRuleStatisticsRepository _ruleStatisticsRepository;
    private readonly IDifyUsageLogRepository _difyUsageLogRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        IRuleStatisticsRepository ruleStatisticsRepository,
        IDifyUsageLogRepository difyUsageLogRepository,
        IRuleRepository ruleRepository,
        ILogger<StatisticsService> logger)
    {
        _ruleStatisticsRepository = ruleStatisticsRepository;
        _difyUsageLogRepository = difyUsageLogRepository;
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var (totalTokens, totalCost) = await _difyUsageLogRepository.GetUsageSummaryAsync(fromDate, toDate, cancellationToken);

        return new UsageSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalTokensConsumed = totalTokens,
            TotalCost = totalCost
        };
    }

    public async Task<IEnumerable<RuleStatisticsDto>> GetTopTriggeredRulesAsync(DateOnly fromDate, DateOnly toDate, int top = 10, CancellationToken cancellationToken = default)
    {
        var statistics = await _ruleStatisticsRepository.GetTopTriggeredRulesAsync(fromDate, toDate, top, cancellationToken);
        var rules = (await _ruleRepository.GetAllActiveAsync(cancellationToken)).ToDictionary(r => r.Id);

        return statistics.Select(s =>
        {
            rules.TryGetValue(s.RuleId, out var rule);
            return new RuleStatisticsDto
            {
                RuleId = s.RuleId,
                RuleName = rule?.Name ?? "Unknown",
                RuleType = rule?.Type.ToString() ?? "Unknown",
                TotalTriggers = s.TriggerCount,
                TotalComments = s.CommentGeneratedCount,
                TotalPassed = s.PassedCount,
                CommentRate = s.TriggerCount > 0 ? (double)s.CommentGeneratedCount / s.TriggerCount * 100 : 0,
                TotalTokens = s.TotalTokensConsumed,
                AverageTokensPerTrigger = s.AverageTokensPerTrigger
            };
        });
    }

    public async Task<IEnumerable<RuleCostDto>> GetCostByRuleAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var costByRule = await _difyUsageLogRepository.GetCostByRuleAsync(fromDate, toDate, cancellationToken);
        var totalCost = costByRule.Sum(c => c.Cost);

        return costByRule.Select(c => new RuleCostDto
        {
            RuleId = c.RuleId,
            RuleName = c.RuleName,
            TotalTokens = c.Tokens,
            TotalCost = c.Cost,
            CostPercentage = totalCost > 0 ? (double)(c.Cost / totalCost * 100) : 0
        });
    }
}
