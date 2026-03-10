namespace NineYi.Ai.CodeReview.Application.Options;

/// <summary>
/// 各平台憑證設定（Webhook Secret + API Access Token）。
/// 實際值請寫在 secrets config（環境變數或 Key Vault），不要寫在 appsettings.json。
/// 環境變數 key 格式（雙底線代表巢狀）：
///   _N1SECRETS__GitHub__WebhookSecret
///   _N1SECRETS__GitHub__AccessToken
///   _N1SECRETS__GitLab__WebhookSecret
///   _N1SECRETS__GitLab__AccessToken
///   _N1SECRETS__GitLab__ApiBaseUrl
///   _N1SECRETS__Bitbucket__AccessToken
///   _N1SECRETS__Bitbucket__Username
/// </summary>
public class WebhookSecretsOptions
{
    public const string SectionName = "_N1SECRETS";

    public GitHubSecretOptions GitHub { get; set; } = new();
    public GitLabSecretOptions GitLab { get; set; } = new();
    public BitbucketSecretOptions Bitbucket { get; set; } = new();

    public class GitHubSecretOptions
    {
        /// <summary>
        /// GitHub Webhook Secret。
        /// 用於驗證 X-Hub-Signature-256 header（HMAC-SHA256）。
        /// 為空時跳過驗證。
        /// </summary>
        public string? WebhookSecret { get; set; }

        /// <summary>
        /// GitHub API Access Token（Personal Access Token 或 GitHub App Token）。
        /// 用於呼叫 GitHub REST API 取得 PR diff 與 raw content。
        /// </summary>
        public string? AccessToken { get; set; }
    }

    public class GitLabSecretOptions
    {
        /// <summary>
        /// GitLab Webhook Token。
        /// 用於驗證 X-Gitlab-Token header（直接比對）。
        /// 為空時跳過驗證。
        /// </summary>
        public string? WebhookSecret { get; set; }

        /// <summary>
        /// GitLab Personal Access Token。
        /// 用於呼叫 GitLab API 取得 MR diff 與 raw content（PRIVATE-TOKEN header）。
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// GitLab API base URL。自架 GitLab 時填入，例如 "https://gitlab.mycompany.com"。
        /// 為空時預設使用 "https://gitlab.com"。
        /// </summary>
        public string? ApiBaseUrl { get; set; }
    }

    public class BitbucketSecretOptions
    {
        /// <summary>
        /// Bitbucket API Access Token（App Password 或 Repository Access Token）。
        /// 用於呼叫 Bitbucket API 取得 PR diff 與 raw content。
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Bitbucket 使用者名稱。App Password 搭配 Basic Auth 時需要。
        /// </summary>
        public string? Username { get; set; }
    }
}
