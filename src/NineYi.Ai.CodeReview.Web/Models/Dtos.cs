namespace NineYi.Ai.CodeReview.Web.Models;

// Repository DTOs
public class RepositoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Platform { get; set; }
    public string PlatformRepositoryId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RuleCount { get; set; }
}

public class CreateRepositoryRequest
{
    public string FullName { get; set; } = string.Empty;
    public int Platform { get; set; } = 1;
}

public class UpdateRepositoryRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

// Platform Settings DTOs
public class PlatformSettingsDto
{
    public Guid Id { get; set; }
    public int Platform { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public bool HasAccessToken { get; set; }
    public bool HasWebhookSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpsertPlatformSettingsRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
}

public class RepositoryLookupResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultBranch { get; set; }
    public bool Private { get; set; }
}

// Rule DTOs
public class RuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DifyApiEndpoint { get; set; }
    public int Type { get; set; }
    public int Priority { get; set; }
    public string? FilePatterns { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DifyApiEndpoint { get; set; }
    public string DifyApiKey { get; set; } = string.Empty;
    public int Type { get; set; } = 1;
    public int Priority { get; set; } = 100;
    public string? FilePatterns { get; set; }
}

public class UpdateRuleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DifyApiEndpoint { get; set; }
    public string? DifyApiKey { get; set; }
    public int? Type { get; set; }
    public int? Priority { get; set; }
    public string? FilePatterns { get; set; }
    public bool? IsActive { get; set; }
}

public class RuleMappingRequest
{
    public int? PriorityOverride { get; set; }
    public string? FilePatternsOverride { get; set; }
}

// HotKeyword DTOs
public class HotKeywordDto
{
    public Guid Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public int Category { get; set; }
    public int Severity { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string? FilePatterns { get; set; }
    public int TriggerCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateHotKeywordRequest
{
    public string Keyword { get; set; } = string.Empty;
    public int Category { get; set; } = 1;
    public int Severity { get; set; } = 2;
    public string AlertMessage { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string? FilePatterns { get; set; }
}

public class UpdateHotKeywordRequest
{
    public string? Keyword { get; set; }
    public int? Category { get; set; }
    public int? Severity { get; set; }
    public string? AlertMessage { get; set; }
    public bool? IsRegex { get; set; }
    public string? FilePatterns { get; set; }
    public bool? IsActive { get; set; }
}

// Statistics DTOs
public class UsageSummaryDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalTokensConsumed { get; set; }
    public decimal TotalCost { get; set; }
}

public class RuleStatisticsDto
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public int TotalTriggers { get; set; }
    public int TotalComments { get; set; }
    public int TotalPassed { get; set; }
    public double CommentRate { get; set; }
    public int TotalTokens { get; set; }
    public int AverageTokensPerTrigger { get; set; }
}

// Enum helpers
public static class EnumHelpers
{
    public static string GetPlatformName(int platform) => platform switch
    {
        1 => "GitHub",
        2 => "GitLab",
        3 => "Bitbucket",
        _ => "Unknown"
    };

    public static string GetRuleTypeName(int type) => type switch
    {
        1 => "Security",
        2 => "CodeStyle",
        3 => "Performance",
        4 => "BestPractice",
        5 => "Documentation",
        6 => "Testing",
        99 => "Custom",
        _ => "Unknown"
    };

    public static string GetCategoryName(int category) => category switch
    {
        1 => "Security",
        2 => "Performance",
        3 => "Deprecated",
        4 => "Configuration",
        5 => "Sensitive",
        99 => "Custom",
        _ => "Unknown"
    };

    public static string GetSeverityName(int severity) => severity switch
    {
        1 => "Info",
        2 => "Warning",
        3 => "Error",
        4 => "Critical",
        _ => "Unknown"
    };

    public static string GetSeverityColor(int severity) => severity switch
    {
        1 => "info",
        2 => "warning",
        3 => "danger",
        4 => "danger",
        _ => "secondary"
    };
}
