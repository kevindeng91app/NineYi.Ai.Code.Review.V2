using System.Text.Json;
using NineYi.Ai.CodeReview.Application.DTOs;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Infrastructure.Services.WebhookParsers;

public class GitHubWebhookParser : IWebhookParserService
{
    public GitPlatformType Platform => GitPlatformType.GitHub;

    public WebhookPayload? ParsePayload(string payload, string? eventType = null)
    {
        try
        {
            var json = JsonDocument.Parse(payload);
            var root = json.RootElement;

            var action = root.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : null;

            // 解析 repository
            var repo = root.GetProperty("repository");
            var repository = new WebhookRepository
            {
                Id = repo.GetProperty("id").GetInt64().ToString(),
                Name = repo.GetProperty("name").GetString() ?? string.Empty,
                FullName = repo.GetProperty("full_name").GetString() ?? string.Empty,
                CloneUrl = repo.TryGetProperty("clone_url", out var cloneUrl) ? cloneUrl.GetString() : null
            };

            // 解析 pull request（如果有的話）
            WebhookPullRequest? pullRequest = null;
            if (root.TryGetProperty("pull_request", out var pr))
            {
                pullRequest = new WebhookPullRequest
                {
                    Number = pr.GetProperty("number").GetInt32(),
                    Title = pr.GetProperty("title").GetString() ?? string.Empty,
                    Body = pr.TryGetProperty("body", out var body) ? body.GetString() : null,
                    State = pr.GetProperty("state").GetString() ?? string.Empty,
                    SourceBranch = pr.GetProperty("head").GetProperty("ref").GetString() ?? string.Empty,
                    TargetBranch = pr.GetProperty("base").GetProperty("ref").GetString() ?? string.Empty,
                    HeadSha = pr.GetProperty("head").GetProperty("sha").GetString(),
                    Author = ParseUser(pr.GetProperty("user"))
                };
            }

            // 解析 sender
            WebhookSender? sender = null;
            if (root.TryGetProperty("sender", out var senderProp))
            {
                sender = new WebhookSender
                {
                    Id = senderProp.GetProperty("id").GetInt64().ToString(),
                    Username = senderProp.GetProperty("login").GetString() ?? string.Empty
                };
            }

            return new WebhookPayload
            {
                Platform = GitPlatformType.GitHub,
                EventType = eventType ?? "unknown",
                Action = action ?? string.Empty,
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
        // 只處理 pull_request 事件的 opened, synchronize, reopened 動作
        if (payload.EventType != "pull_request")
            return false;

        var validActions = new[] { "opened", "synchronize", "reopened" };
        return validActions.Contains(payload.Action.ToLower());
    }

    private static WebhookUser? ParseUser(JsonElement userElement)
    {
        return new WebhookUser
        {
            Id = userElement.GetProperty("id").GetInt64().ToString(),
            Username = userElement.GetProperty("login").GetString() ?? string.Empty
        };
    }
}
