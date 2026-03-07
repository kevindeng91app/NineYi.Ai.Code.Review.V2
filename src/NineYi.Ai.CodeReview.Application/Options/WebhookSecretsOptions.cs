namespace NineYi.Ai.CodeReview.Application.Options;

/// <summary>
/// 各平台 Webhook Secret 設定。
/// 實際值請寫在 secrets config（環境變數或 Key Vault），不要寫在 appsettings.json。
/// 對應 config key：_N1SECRETS:GitHub:WebhookSecret、_N1SECRETS:GitLab:WebhookSecret
/// </summary>
public class WebhookSecretsOptions
{
    public const string SectionName = "_N1SECRETS";

    public GitHubSecretOptions GitHub { get; set; } = new();
    public GitLabSecretOptions GitLab { get; set; } = new();

    public class GitHubSecretOptions
    {
        /// <summary>
        /// GitHub Webhook Secret。
        /// 用於驗證 X-Hub-Signature-256 header（HMAC-SHA256）。
        /// 為空時跳過驗證。
        /// </summary>
        public string? WebhookSecret { get; set; }
    }

    public class GitLabSecretOptions
    {
        /// <summary>
        /// GitLab Webhook Token。
        /// 用於驗證 X-Gitlab-Token header（直接比對）。
        /// 為空時跳過驗證。
        /// </summary>
        public string? WebhookSecret { get; set; }
    }
}
