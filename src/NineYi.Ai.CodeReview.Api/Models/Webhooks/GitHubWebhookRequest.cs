using System.Text.Json.Serialization;

namespace NineYi.Ai.CodeReview.Api.Models.Webhooks;

/// <summary>
/// GitHub Pull Request Webhook payload
/// 參考：https://docs.github.com/en/webhooks/webhook-events-and-payloads#pull_request
/// </summary>
public class GitHubWebhookRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>PR 編號（root level，與 pull_request.number 相同）</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("pull_request")]
    public GitHubPrPayload PullRequest { get; set; } = new();

    [JsonPropertyName("repository")]
    public GitHubRepoPayload Repository { get; set; } = new();

    [JsonPropertyName("sender")]
    public GitHubUserPayload Sender { get; set; } = new();
}

public class GitHubPrPayload
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    /// <summary>可直接用來取得 unified diff 的 URL，無需額外 API call</summary>
    [JsonPropertyName("diff_url")]
    public string DiffUrl { get; set; } = string.Empty;

    [JsonPropertyName("head")]
    public GitHubBranchPayload Head { get; set; } = new();

    [JsonPropertyName("base")]
    public GitHubBranchPayload Base { get; set; } = new();

    [JsonPropertyName("user")]
    public GitHubUserPayload User { get; set; } = new();
}

public class GitHubBranchPayload
{
    /// <summary>Branch 名稱</summary>
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    /// <summary>Commit SHA</summary>
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
}

public class GitHubRepoPayload
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"owner/repo" 格式</summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
}

public class GitHubUserPayload
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}
