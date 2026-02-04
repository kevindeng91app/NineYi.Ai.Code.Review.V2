using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Application.DTOs;

/// <summary>
/// 統一的 Webhook Payload 格式
/// </summary>
public class WebhookPayload
{
    public GitPlatformType Platform { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public WebhookRepository Repository { get; set; } = new();
    public WebhookPullRequest? PullRequest { get; set; }
    public WebhookSender? Sender { get; set; }
    public string RawPayload { get; set; } = string.Empty;
}

public class WebhookRepository
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CloneUrl { get; set; }
}

public class WebhookPullRequest
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string State { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string? HeadSha { get; set; }
    public WebhookUser? Author { get; set; }
}

public class WebhookUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class WebhookSender
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
