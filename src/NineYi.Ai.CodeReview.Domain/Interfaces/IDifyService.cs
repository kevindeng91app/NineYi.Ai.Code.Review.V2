namespace NineYi.Ai.CodeReview.Domain.Interfaces;

/// <summary>
/// Dify API 服務介面
/// </summary>
public interface IDifyService
{
    Task<DifyReviewResult> ReviewCodeAsync(DifyReviewRequest request, CancellationToken cancellationToken = default);
    Task<DifyUsageInfo> GetUsageInfoAsync(string apiKey, string endpoint, CancellationToken cancellationToken = default);
}

public class DifyReviewRequest
{
    /// <summary>
    /// Dify API Endpoint（可選，若未設定則使用全域設定）
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Dify API Key（必填，對應不同的 Workflow/App）
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string FileDiff { get; set; } = string.Empty;
    public string? FileContent { get; set; }
    public string? AdditionalContext { get; set; }
}

public class DifyReviewResult
{
    public bool IsSuccess { get; set; }
    public string? RequestId { get; set; }
    public bool HasIssues { get; set; }
    public List<CodeReviewComment> Comments { get; set; } = new();
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public string? ModelName { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
}

public class CodeReviewComment
{
    public int? LineNumber { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // info, warning, error
    public string? Category { get; set; }
    public string? Suggestion { get; set; }
}

public class DifyUsageInfo
{
    public decimal TotalCost { get; set; }
    public int TotalTokens { get; set; }
    public int TotalRequests { get; set; }
    public string? PlanType { get; set; }
    public decimal? RemainingQuota { get; set; }
}
