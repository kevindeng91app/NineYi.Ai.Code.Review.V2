using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly IReviewLogRepository _reviewLogRepository;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsService statisticsService,
        IReviewLogRepository reviewLogRepository,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _reviewLogRepository = reviewLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 取得使用量摘要（包含 Dify 費用）
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<UsageSummaryDto>> GetUsageSummary(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var summary = await _statisticsService.GetUsageSummaryAsync(from, to, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// 取得最常觸發的規則（用於分析 Prompt 精準度）
    /// </summary>
    [HttpGet("top-triggered-rules")]
    public async Task<ActionResult<IEnumerable<RuleStatisticsDto>>> GetTopTriggeredRules(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var statistics = await _statisticsService.GetTopTriggeredRulesAsync(from, to, top, cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// 取得各規則的費用統計
    /// </summary>
    [HttpGet("cost-by-rule")]
    public async Task<ActionResult<IEnumerable<RuleCostDto>>> GetCostByRule(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var costs = await _statisticsService.GetCostByRuleAsync(from, to, cancellationToken);
        return Ok(costs);
    }

    /// <summary>
    /// 取得 Repository 的 Review 紀錄
    /// </summary>
    [HttpGet("reviews/{repositoryId:guid}")]
    public async Task<ActionResult<IEnumerable<ReviewLogDto>>> GetReviewLogs(
        Guid repositoryId,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var logs = await _reviewLogRepository.GetByRepositoryIdAsync(repositoryId, pageSize, page, cancellationToken);
        return Ok(logs.Select(l => new ReviewLogDto
        {
            Id = l.Id,
            PullRequestNumber = l.PullRequestNumber,
            PullRequestTitle = l.PullRequestTitle,
            Author = l.Author,
            Status = l.Status.ToString(),
            StartedAt = l.StartedAt,
            CompletedAt = l.CompletedAt,
            FilesProcessed = l.FilesProcessed,
            CommentsGenerated = l.CommentsGenerated,
            TokensConsumed = l.TokensConsumed,
            EstimatedCost = l.EstimatedCost,
            ErrorMessage = l.ErrorMessage
        }));
    }

    /// <summary>
    /// 取得單一 Review 的詳細紀錄
    /// </summary>
    [HttpGet("reviews/detail/{reviewId:guid}")]
    public async Task<ActionResult<ReviewLogDetailDto>> GetReviewDetail(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        var log = await _reviewLogRepository.GetByIdAsync(reviewId, cancellationToken);
        if (log == null)
            return NotFound();

        return Ok(new ReviewLogDetailDto
        {
            Id = log.Id,
            PullRequestNumber = log.PullRequestNumber,
            PullRequestTitle = log.PullRequestTitle,
            Author = log.Author,
            Status = log.Status.ToString(),
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            FilesProcessed = log.FilesProcessed,
            CommentsGenerated = log.CommentsGenerated,
            TokensConsumed = log.TokensConsumed,
            EstimatedCost = log.EstimatedCost,
            ErrorMessage = log.ErrorMessage,
            DetailedLog = log.DetailedLog,
            FileLogs = log.FileLogs.Select(f => new ReviewFileLogDto
            {
                Id = f.Id,
                FilePath = f.FilePath,
                ChangeType = f.ChangeType.ToString(),
                LinesAdded = f.LinesAdded,
                LinesDeleted = f.LinesDeleted,
                HasComments = f.HasComments,
                Comments = f.Comments,
                MatchedKeywords = f.MatchedKeywords,
                TokensConsumed = f.TokensConsumed,
                ProcessedAt = f.ProcessedAt
            }).ToList()
        });
    }
}

public class ReviewLogDto
{
    public Guid Id { get; set; }
    public int PullRequestNumber { get; set; }
    public string PullRequestTitle { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int FilesProcessed { get; set; }
    public int CommentsGenerated { get; set; }
    public int TokensConsumed { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ReviewLogDetailDto : ReviewLogDto
{
    public string? DetailedLog { get; set; }
    public List<ReviewFileLogDto> FileLogs { get; set; } = new();
}

public class ReviewFileLogDto
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public bool HasComments { get; set; }
    public string? Comments { get; set; }
    public string? MatchedKeywords { get; set; }
    public int TokensConsumed { get; set; }
    public DateTime ProcessedAt { get; set; }
}
