using System.Text.Json;
using NineYi.Ai.CodeReview.Application.DTOs;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Infrastructure.Services.WebhookParsers;

public class BitbucketWebhookParser : IWebhookParserService
{
    public GitPlatformType Platform => GitPlatformType.Bitbucket;

    public WebhookPayload? ParsePayload(string payload, string? eventType = null)
    {
        try
        {
            var json = JsonDocument.Parse(payload);
            var root = json.RootElement;

            // 解析 repository
            var repo = root.GetProperty("repository");
            var repository = new WebhookRepository
            {
                Id = repo.GetProperty("uuid").GetString()?.Trim('{', '}') ?? string.Empty,
                Name = repo.GetProperty("name").GetString() ?? string.Empty,
                FullName = repo.GetProperty("full_name").GetString() ?? string.Empty
            };

            // 解析 pull request（如果有的話）
            WebhookPullRequest? pullRequest = null;
            if (root.TryGetProperty("pullrequest", out var pr))
            {
                pullRequest = new WebhookPullRequest
                {
                    Number = pr.GetProperty("id").GetInt32(),
                    Title = pr.GetProperty("title").GetString() ?? string.Empty,
                    Body = pr.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    State = pr.GetProperty("state").GetString() ?? string.Empty,
                    SourceBranch = pr.GetProperty("source").GetProperty("branch").GetProperty("name").GetString() ?? string.Empty,
                    TargetBranch = pr.GetProperty("destination").GetProperty("branch").GetProperty("name").GetString() ?? string.Empty,
                    HeadSha = pr.GetProperty("source").TryGetProperty("commit", out var commit) &&
                              commit.TryGetProperty("hash", out var hash)
                        ? hash.GetString()
                        : null,
                    Author = ParseUser(pr.GetProperty("author"))
                };
            }

            // 解析 actor
            WebhookSender? sender = null;
            if (root.TryGetProperty("actor", out var actor))
            {
                sender = new WebhookSender
                {
                    Id = actor.TryGetProperty("uuid", out var uuid) ? uuid.GetString()?.Trim('{', '}') ?? string.Empty : string.Empty,
                    Username = actor.TryGetProperty("nickname", out var nickname)
                        ? nickname.GetString() ?? string.Empty
                        : actor.TryGetProperty("display_name", out var displayName)
                            ? displayName.GetString() ?? string.Empty
                            : string.Empty
                };
            }

            // Bitbucket 的 event type 通常是透過 header 傳入（X-Event-Key）
            // 格式如：pullrequest:created, pullrequest:updated
            var action = eventType?.Contains(':') == true
                ? eventType.Split(':').Last()
                : string.Empty;

            return new WebhookPayload
            {
                Platform = GitPlatformType.Bitbucket,
                EventType = eventType ?? "unknown",
                Action = action,
                Repository = repository,
                PullRequest = pullRequest,
                Sender = sender,
                RawPayload = payload
            };
        }
        catch
        {
            return null;
        }
    }

    public bool ShouldProcess(WebhookPayload payload)
    {
        // 只處理 pullrequest 相關事件
        if (!payload.EventType.StartsWith("pullrequest:"))
            return false;

        var validActions = new[] { "created", "updated" };
        return validActions.Contains(payload.Action.ToLower());
    }

    private static WebhookUser? ParseUser(JsonElement userElement)
    {
        return new WebhookUser
        {
            Id = userElement.TryGetProperty("uuid", out var uuid) ? uuid.GetString()?.Trim('{', '}') ?? string.Empty : string.Empty,
            Username = userElement.TryGetProperty("nickname", out var nickname)
                ? nickname.GetString() ?? string.Empty
                : userElement.TryGetProperty("display_name", out var displayName)
                    ? displayName.GetString() ?? string.Empty
                    : string.Empty
        };
    }
}
