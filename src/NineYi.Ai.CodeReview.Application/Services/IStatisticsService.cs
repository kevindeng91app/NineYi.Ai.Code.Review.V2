using NineYi.Ai.CodeReview.Application.DTOs;

namespace NineYi.Ai.CodeReview.Application.Services;

public interface IStatisticsService
{
    Task<UsageSummaryDto> GetUsageSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuleStatisticsDto>> GetTopTriggeredRulesAsync(DateOnly fromDate, DateOnly toDate, int top = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuleCostDto>> GetCostByRuleAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}

public class UsageSummaryDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalReviews { get; set; }
    public int TotalFilesProcessed { get; set; }
    public int TotalCommentsGenerated { get; set; }
    public int TotalTokensConsumed { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AverageCostPerReview { get; set; }
}

public class RuleStatisticsDto
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public int TotalTriggers { get; set; }
    public int TotalComments { get; set; }
    public int TotalPassed { get; set; }
    public double CommentRate { get; set; } // 產生評論的比率
    public long TotalTokens { get; set; }
    public double AverageTokensPerTrigger { get; set; }
}

public class RuleCostDto
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double CostPercentage { get; set; }
}
