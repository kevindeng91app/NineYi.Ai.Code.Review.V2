using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IEnumerable<IWebhookParserService> _parsers;
    private readonly ICodeReviewService _codeReviewService;
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IGitPlatformServiceFactory _gitPlatformServiceFactory;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IEnumerable<IWebhookParserService> parsers,
        ICodeReviewService codeReviewService,
        IPlatformSettingsRepository platformSettingsRepository,
        IGitPlatformServiceFactory gitPlatformServiceFactory,
        ILogger<WebhookController> logger)
    {
        _parsers = parsers;
        _codeReviewService = codeReviewService;
        _platformSettingsRepository = platformSettingsRepository;
        _gitPlatformServiceFactory = gitPlatformServiceFactory;
        _logger = logger;
    }

    /// <summary>
    /// GitHub Webhook endpoint
    /// </summary>
    [HttpPost("github")]
    public async Task<IActionResult> GitHub(CancellationToken cancellationToken)
    {
        var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

        _logger.LogInformation("Received GitHub webhook: Event={EventType}, DeliveryId={DeliveryId}",
            eventType, deliveryId);

        return await ProcessWebhook(GitPlatformType.GitHub, eventType, signature, cancellationToken);
    }

    /// <summary>
    /// GitLab Webhook endpoint
    /// </summary>
    [HttpPost("gitlab")]
    public async Task<IActionResult> GitLab(CancellationToken cancellationToken)
    {
        var eventType = Request.Headers["X-Gitlab-Event"].FirstOrDefault();
        var token = Request.Headers["X-Gitlab-Token"].FirstOrDefault();

        _logger.LogInformation("Received GitLab webhook: Event={EventType}", eventType);

        return await ProcessWebhook(GitPlatformType.GitLab, eventType, token, cancellationToken);
    }

    /// <summary>
    /// Bitbucket Webhook endpoint
    /// </summary>
    [HttpPost("bitbucket")]
    public async Task<IActionResult> Bitbucket(CancellationToken cancellationToken)
    {
        var eventType = Request.Headers["X-Event-Key"].FirstOrDefault();
        var hookUuid = Request.Headers["X-Hook-UUID"].FirstOrDefault();

        _logger.LogInformation("Received Bitbucket webhook: Event={EventType}, HookUUID={HookUUID}",
            eventType, hookUuid);

        return await ProcessWebhook(GitPlatformType.Bitbucket, eventType, null, cancellationToken);
    }

    private async Task<IActionResult> ProcessWebhook(
        GitPlatformType platform,
        string? eventType,
        string? signature,
        CancellationToken cancellationToken)
    {
        try
        {
            // 讀取請求內容
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);

            // 找到對應的 parser
            var parser = _parsers.FirstOrDefault(p => p.Platform == platform);
            if (parser == null)
            {
                _logger.LogWarning("No parser found for platform {Platform}", platform);
                return BadRequest($"Unsupported platform: {platform}");
            }

            // 解析 payload
            var webhookPayload = parser.ParsePayload(payload, eventType);
            if (webhookPayload == null)
            {
                _logger.LogWarning("Failed to parse webhook payload for platform {Platform}", platform);
                return BadRequest("Invalid webhook payload");
            }

            // 檢查是否需要處理此事件
            if (!parser.ShouldProcess(webhookPayload))
            {
                _logger.LogInformation("Skipping webhook event: {EventType}/{Action}",
                    webhookPayload.EventType, webhookPayload.Action);
                return Ok(new { message = "Event skipped", reason = "Not a processable event" });
            }

            // 驗證 webhook signature（如果有設定的話）
            if (!string.IsNullOrEmpty(signature))
            {
                var platformSettings = await _platformSettingsRepository.GetByPlatformAsync(platform, cancellationToken);

                if (platformSettings != null && !string.IsNullOrEmpty(platformSettings.WebhookSecret))
                {
                    var gitService = _gitPlatformServiceFactory.GetService(platform);
                    if (!gitService.ValidateWebhookSignature(payload, signature, platformSettings.WebhookSecret))
                    {
                        _logger.LogWarning("Invalid webhook signature for repository {Repository}",
                            webhookPayload.Repository.FullName);
                        return Unauthorized("Invalid webhook signature");
                    }
                }
            }

            // 異步處理 code review（不阻塞 webhook 回應）
            _ = Task.Run(async () =>
            {
                try
                {
                    await _codeReviewService.ProcessPullRequestAsync(webhookPayload, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PR #{PrNumber} for {Repository}",
                        webhookPayload.PullRequest?.Number, webhookPayload.Repository.FullName);
                }
            });

            return Ok(new
            {
                message = "Webhook received and processing started",
                repository = webhookPayload.Repository.FullName,
                pullRequest = webhookPayload.PullRequest?.Number
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for platform {Platform}", platform);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
