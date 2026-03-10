using System.Text.Json.Serialization;

namespace NineYi.Ai.CodeReview.Api.Models.Webhooks;

/// <summary>
/// GitLab Merge Request Webhook payload
/// 參考：https://docs.gitlab.com/ee/user/project/integrations/webhook_events.html#merge-request-events
/// 注意：GitLab payload 不包含 diff_url，Phase 2 由 GitLabClient 以 project.id + iid 呼叫 API 取得
/// </summary>
public class GitLabWebhookRequest
{
    /// <summary>事件種類，必須為 "merge_request"</summary>
    [JsonPropertyName("object_kind")]
    public string ObjectKind { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public GitLabUserPayload User { get; set; } = new();

    [JsonPropertyName("project")]
    public GitLabProjectPayload Project { get; set; } = new();

    [JsonPropertyName("object_attributes")]
    public GitLabObjectAttributesPayload ObjectAttributes { get; set; } = new();
}

public class GitLabUserPayload
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class GitLabProjectPayload
{
    /// <summary>GitLab Project 數字 ID，Phase 2 呼叫 API 時使用</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"group/repo" 格式，對應 RepoFullName</summary>
    [JsonPropertyName("path_with_namespace")]
    public string PathWithNamespace { get; set; } = string.Empty;

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = string.Empty;
}

public class GitLabObjectAttributesPayload
{
    /// <summary>
    /// MR 的 internal ID（iid），在 project 內唯一。
    /// 注意：使用 iid 而非 id，與 GitLab API 呼叫一致
    /// </summary>
    [JsonPropertyName("iid")]
    public int Iid { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>opened / closed / merged</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>open / update / reopen / close / merge</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("source_branch")]
    public string SourceBranch { get; set; } = string.Empty;

    [JsonPropertyName("target_branch")]
    public string TargetBranch { get; set; } = string.Empty;

    /// <summary>Draft MR 旗標（GitLab 15.0+）</summary>
    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("last_commit")]
    public GitLabLastCommitPayload LastCommit { get; set; } = new();
}

public class GitLabLastCommitPayload
{
    /// <summary>Head commit SHA，Phase 2 抓 raw content 時使用</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
