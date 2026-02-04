using NineYi.Ai.CodeReview.Application.DTOs;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Application.Services;

public interface IWebhookParserService
{
    GitPlatformType Platform { get; }
    WebhookPayload? ParsePayload(string payload, string? eventType = null);
    bool ShouldProcess(WebhookPayload payload);
}
