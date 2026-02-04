using System.Text.Json;
using NineYi.Ai.CodeReview.Application.DTOs;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Infrastructure.Services.WebhookParsers;

public class GitLabWebhookParser : IWebhookParserService
{
    public GitPlatformType Platform => GitPlatformType.GitLab;

    public WebhookPayload? ParsePayload(string payload, string? eventType = null)
    {
        try
        {
            var json = JsonDocument.Parse(payload);
            var root = json.RootElement;

            var objectKind = root.TryGetProperty("object_kind", out var kindProp) ? kindProp.GetString() : null;
            var action = root.TryGetProperty("object_attributes", out var objAttr) &&
                         objAttr.TryGetProperty("action", out var actionProp)
                ? actionProp.GetString()
                : null;

            // 解析 project (GitLab 中對應 repository)
            var project = root.GetProperty("project");
            var repository = new WebhookRepository
            {
                Id = project.GetProperty("id").GetInt64().ToString(),
                Name = project.GetProperty("name").GetString() ?? string.Empty,
                FullName = project.GetProperty("path_with_namespace").GetString() ?? string.Empty,
                CloneUrl = project.TryGetProperty("git_http_url", out var cloneUrl) ? cloneUrl.GetString() : null
            };

            // 解析 merge request（如果有的話）
            WebhookPullRequest? pullRequest = null;
            if (root.TryGetProperty("object_attributes", out var mr) && objectKind == "merge_request")
            {
                pullRequest = new WebhookPullRequest
                {
                    Number = mr.GetProperty("iid").GetInt32(),
                    Title = mr.GetProperty("title").GetString() ?? string.Empty,
                    Body = mr.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    State = mr.GetProperty("state").GetString() ?? string.Empty,
                    SourceBranch = mr.GetProperty("source_branch").GetString() ?? string.Empty,
                    TargetBranch = mr.GetProperty("target_branch").GetString() ?? string.Empty,
                    HeadSha = mr.TryGetProperty("last_commit", out var lastCommit) &&
                              lastCommit.TryGetProperty("id", out var sha)
                        ? sha.GetString()
                        : null
                };

                // 解析作者
                if (mr.TryGetProperty("author_id", out var authorId))
                {
                    pullRequest.Author = new WebhookUser
                    {
                        Id = authorId.GetInt64().ToString()
                    };
                }
            }

            // 解析 user
            WebhookSender? sender = null;
            if (root.TryGetProperty("user", out var user))
            {
                sender = new WebhookSender
                {
                    Id = user.TryGetProperty("id", out var userId) ? userId.GetInt64().ToString() : string.Empty,
                    Username = user.TryGetProperty("username", out var username) ? username.GetString() ?? string.Empty : string.Empty
                };

                // 如果 PR 的作者沒有 username，使用 sender 的
                if (pullRequest?.Author != null && string.IsNullOrEmpty(pullRequest.Author.Username))
                {
                    pullRequest.Author.Username = sender.Username;
                }
            }

            return new WebhookPayload
            {
                Platform = GitPlatformType.GitLab,
                EventType = objectKind ?? eventType ?? "unknown",
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
        // 只處理 merge_request 事件的 open, reopen, update 動作
        if (payload.EventType != "merge_request")
            return false;

        var validActions = new[] { "open", "reopen", "update" };
        return validActions.Contains(payload.Action.ToLower());
    }
}
