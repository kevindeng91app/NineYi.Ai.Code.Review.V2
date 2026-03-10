using System.Text.Json.Serialization;

namespace NineYi.Ai.CodeReview.Api.Models.Webhooks;

/// <summary>
/// Bitbucket Pull Request Webhook payload
/// 參考：https://support.atlassian.com/bitbucket-cloud/docs/event-payloads/#Pull-request-events
/// </summary>
public class BitbucketWebhookRequest
{
    [JsonPropertyName("repository")]
    public BitbucketRepoPayload Repository { get; set; } = new();

    [JsonPropertyName("actor")]
    public BitbucketActorPayload Actor { get; set; } = new();

    [JsonPropertyName("pullrequest")]
    public BitbucketPrPayload Pullrequest { get; set; } = new();
}

public class BitbucketRepoPayload
{
    /// <summary>"workspace/repo-slug" 格式，對應 RepoFullName</summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;
}

public class BitbucketActorPayload
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

public class BitbucketPrPayload
{
    /// <summary>PR 編號</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>OPEN / MERGED / DECLINED</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("source")]
    public BitbucketBranchRefPayload Source { get; set; } = new();

    [JsonPropertyName("destination")]
    public BitbucketBranchRefPayload Destination { get; set; } = new();

    [JsonPropertyName("author")]
    public BitbucketActorPayload Author { get; set; } = new();

    [JsonPropertyName("links")]
    public BitbucketPrLinksPayload Links { get; set; } = new();
}

public class BitbucketBranchRefPayload
{
    [JsonPropertyName("branch")]
    public BitbucketBranchPayload Branch { get; set; } = new();

    [JsonPropertyName("commit")]
    public BitbucketCommitPayload Commit { get; set; } = new();
}

public class BitbucketBranchPayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BitbucketCommitPayload
{
    /// <summary>Commit hash，對應 HeadCommitSha</summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
}

public class BitbucketPrLinksPayload
{
    /// <summary>Diff URL，可直接用來取得 diff 內容（需 Bearer Token）</summary>
    [JsonPropertyName("diff")]
    public BitbucketHrefPayload Diff { get; set; } = new();

    [JsonPropertyName("diffstat")]
    public BitbucketHrefPayload Diffstat { get; set; } = new();
}

public class BitbucketHrefPayload
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;
}
