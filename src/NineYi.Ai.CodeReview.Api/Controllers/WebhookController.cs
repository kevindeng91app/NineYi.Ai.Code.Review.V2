using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.Options;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Api.Models.Webhooks;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Api.Controllers;

/// <summary>
/// 接收各 Git 平台 Webhook 事件的進入點。
/// 負責事件過濾（ShouldProcess）、Signature 驗證，並組裝 <see cref="StartCodeReviewCommand"/> 交給 Service 處理。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    /// <summary>
    /// The code review service
    /// </summary>
    private readonly ICodeReviewService _codeReviewService;

    /// <summary>
    /// The secrets
    /// </summary>
    private readonly WebhookSecretsOptions _secrets;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger<WebhookController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookController"/> class.
    /// </summary>
    /// <param name="codeReviewService">The code review service.</param>
    /// <param name="secrets">The secrets.</param>
    /// <param name="logger">The logger.</param>
    public WebhookController(
        ICodeReviewService codeReviewService,
        IOptions<WebhookSecretsOptions> secrets,
        ILogger<WebhookController> logger)
    {
        this._codeReviewService = codeReviewService;
        this._secrets = secrets.Value;
        this._logger = logger;
    }

    /// <summary>
    /// 接收 GitHub Pull Request Webhook。
    /// <para>只處理 <c>pull_request</c> 事件中 action 為 opened / synchronize / reopened 的請求。</para>
    /// <para>若設定了 WebhookSecret，會驗證 <c>X-Hub-Signature-256</c> Header（HMAC-SHA256）。</para>
    /// </summary>
    [HttpPost("github")]
    public async Task<IActionResult> GitHub(
        [FromBody] GitHubWebhookRequest body,
        CancellationToken cancellationToken)
    {
        var eventHeader = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

        this._logger.LogInformation(
            "Received GitHub webhook: Event={Event}, Action={Action}, DeliveryId={DeliveryId}",
            eventHeader, body.Action, deliveryId);

        if (!IsPullRequestEvent(eventHeader, body.Action) || body.PullRequest is null)
        {
            this._logger.LogInformation("GitHub webhook skipped: Event={Event}, Action={Action}",
                eventHeader, body.Action);
            return Ok(new { message = "Event skipped" });
        }

        if (!await ValidateGitHubSignatureAsync(body.Repository.FullName))
            return Unauthorized("Invalid webhook signature");

        var command = new StartCodeReviewCommand
        {
            ProviderType = GitPlatformType.GitHub,
            RepoFullName = body.Repository.FullName,
            PullRequestNumber = body.Number,
            Title = body.PullRequest.Title,
            PullRequestRef = new PullRequestRef
            {
                HeadCommitSha = body.PullRequest.Head.Sha,
                SourceBranch = body.PullRequest.Head.Ref,
                TargetBranch = body.PullRequest.Base.Ref,
                DiffUrl = body.PullRequest.DiffUrl
            }
        };

        var result = await this._codeReviewService.StartAsync(command, cancellationToken);

        return Ok(new
        {
            message = "Webhook received",
            reviewLogId = result.ReviewLogId,
            repository = command.RepoFullName,
            pullRequest = command.PullRequestNumber
        });
    }

    /// <summary>
    /// 接收 GitLab Merge Request Webhook。
    /// <para>只處理 <c>Merge Request Hook</c> 事件中 action 為 open / update / reopen 的請求。</para>
    /// <para>若設定了 WebhookSecret，會驗證 <c>X-Gitlab-Token</c> Header（直接比對）。</para>
    /// </summary>
    [HttpPost("gitlab")]
    public async Task<IActionResult> GitLab(
        [FromBody] GitLabWebhookRequest body,
        CancellationToken cancellationToken)
    {
        var eventHeader = Request.Headers["X-Gitlab-Event"].FirstOrDefault();

        this._logger.LogInformation(
            "Received GitLab webhook: Event={Event}, Action={Action}",
            eventHeader, body.ObjectAttributes?.Action);

        if (!IsMergeRequestEvent(eventHeader, body.ObjectKind, body.ObjectAttributes?.Action))
        {
            this._logger.LogInformation("GitLab webhook skipped: Event={Event}, Action={Action}",
                eventHeader, body.ObjectAttributes?.Action);
            return Ok(new { message = "Event skipped" });
        }

        if (!ValidateGitLabToken(body.Project?.PathWithNamespace))
            return Unauthorized("Invalid webhook token");

        var command = new StartCodeReviewCommand
        {
            ProviderType = GitPlatformType.GitLab,
            RepoFullName = body.Project?.PathWithNamespace ?? string.Empty,
            PullRequestNumber = body.ObjectAttributes!.Iid,
            Title = body.ObjectAttributes.Title,
            PlatformProjectId = body.Project?.Id.ToString(),
            PullRequestRef = new PullRequestRef
            {
                HeadCommitSha = body.ObjectAttributes.LastCommit?.Id ?? string.Empty,
                SourceBranch = body.ObjectAttributes.SourceBranch,
                TargetBranch = body.ObjectAttributes.TargetBranch,
                DiffUrl = null   // GitLab webhook 不提供 diff_url，Phase 2 由 GitLabClient 建構
            }
        };

        var result = await this._codeReviewService.StartAsync(command, cancellationToken);

        return Ok(new
        {
            message = "Webhook received",
            reviewLogId = result.ReviewLogId,
            repository = command.RepoFullName,
            pullRequest = command.PullRequestNumber
        });
    }

    /// <summary>
    /// 接收 Bitbucket Pull Request Webhook。
    /// <para>只處理 <c>pullrequest:created</c> 與 <c>pullrequest:updated</c> 且 State 為 OPEN 的請求。</para>
    /// <para>Bitbucket Cloud 不提供內建 Signature，此端點不做簽章驗證。</para>
    /// </summary>
    [HttpPost("bitbucket")]
    public async Task<IActionResult> Bitbucket(
        [FromBody] BitbucketWebhookRequest body,
        CancellationToken cancellationToken)
    {
        var eventKey = Request.Headers["X-Event-Key"].FirstOrDefault();
        var hookUuid = Request.Headers["X-Hook-UUID"].FirstOrDefault();

        this._logger.LogInformation(
            "Received Bitbucket webhook: EventKey={EventKey}, HookUUID={HookUUID}",
            eventKey, hookUuid);

        if (!IsPullRequestEventKey(eventKey) || body.Pullrequest is null || body.Pullrequest.State != "OPEN")
        {
            this._logger.LogInformation("Bitbucket webhook skipped: EventKey={EventKey}, State={State}",
                eventKey, body.Pullrequest?.State);
            return Ok(new { message = "Event skipped" });
        }

        var command = new StartCodeReviewCommand
        {
            ProviderType = GitPlatformType.Bitbucket,
            RepoFullName = body.Repository.FullName,
            PullRequestNumber = body.Pullrequest.Id,
            Title = body.Pullrequest.Title,
            PullRequestRef = new PullRequestRef
            {
                HeadCommitSha = body.Pullrequest.Source.Commit.Hash,
                SourceBranch = body.Pullrequest.Source.Branch.Name,
                TargetBranch = body.Pullrequest.Destination.Branch.Name,
                DiffUrl = body.Pullrequest.Links.Diff.Href
            }
        };

        var result = await this._codeReviewService.StartAsync(command, cancellationToken);

        return Ok(new
        {
            message = "Webhook received",
            reviewLogId = result.ReviewLogId,
            repository = command.RepoFullName,
            pullRequest = command.PullRequestNumber
        });
    }

    // ── ShouldProcess helpers ─────────────────────────────────────────────────

    /// <summary>
    /// 判斷 GitHub 事件是否為需要處理的 PR 事件。
    /// </summary>
    private static bool IsPullRequestEvent(string? eventHeader, string? action) =>
        eventHeader == "pull_request"
        && action is "opened" or "synchronize" or "reopened";

    /// <summary>
    /// 判斷 GitLab 事件是否為需要處理的 MR 事件。
    /// </summary>
    private static bool IsMergeRequestEvent(string? eventHeader, string? objectKind, string? action) =>
        eventHeader == "Merge Request Hook"
        && objectKind == "merge_request"
        && action is "open" or "update" or "reopen";

    /// <summary>
    /// 判斷 Bitbucket 事件 key 是否為需要處理的 PR 事件。
    /// </summary>
    private static bool IsPullRequestEventKey(string? eventKey) =>
        eventKey is "pullrequest:created" or "pullrequest:updated";

    // ── Signature validation helpers ──────────────────────────────────────────

    /// <summary>
    /// 驗證 GitHub Webhook Signature（HMAC-SHA256）。
    /// 未設定 Secret 時直接放行，驗證失敗時回傳 false。
    /// 使用 <see cref="CryptographicOperations.FixedTimeEquals"/> 做常數時間比對，防止 timing attack。
    /// </summary>
    private async Task<bool> ValidateGitHubSignatureAsync(string repoFullName)
    {
        if (this._secrets.GitHub.WebhookSecret is not { Length: > 0 })
            return true;

        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var rawBody = await ReadRawBodyAsync();

        var computed = ComputeGitHubSignature(rawBody, this._secrets.GitHub.WebhookSecret);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature ?? string.Empty)))
        {
            this._logger.LogWarning("Invalid GitHub webhook signature for repo {Repo}", repoFullName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 驗證 GitLab Webhook Token（直接比對 X-Gitlab-Token Header）。
    /// 未設定 Secret 時直接放行，驗證失敗時回傳 false。
    /// </summary>
    private bool ValidateGitLabToken(string? repoFullName)
    {
        if (this._secrets.GitLab.WebhookSecret is not { Length: > 0 })
            return true;

        var token = Request.Headers["X-Gitlab-Token"].FirstOrDefault();
        if (token != this._secrets.GitLab.WebhookSecret)
        {
            this._logger.LogWarning("Invalid GitLab webhook token for repo {Repo}", repoFullName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 計算 GitHub HMAC-SHA256 Signature 字串（格式：<c>sha256=hex</c>）。
    /// 使用 <see cref="CryptographicOperations.FixedTimeEquals"/> 防止 timing attack。
    /// </summary>
    private static string ComputeGitHubSignature(string rawBody, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 重新讀取 Request Body 原始字串。
    /// 需搭配 <c>EnableBuffering</c> middleware 使用，讓 body 可被重複讀取。
    /// </summary>
    private async Task<string> ReadRawBodyAsync()
    {
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
