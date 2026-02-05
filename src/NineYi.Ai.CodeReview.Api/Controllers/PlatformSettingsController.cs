using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/platform-settings")]
public class PlatformSettingsController : ControllerBase
{
    private readonly IPlatformSettingsRepository _settingsRepository;
    private readonly ILogger<PlatformSettingsController> _logger;

    public PlatformSettingsController(
        IPlatformSettingsRepository settingsRepository,
        ILogger<PlatformSettingsController> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有平台設定
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlatformSettingsDto>>> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAllAsync(cancellationToken);
        return Ok(settings.Select(MapToDto));
    }

    /// <summary>
    /// 取得指定平台設定
    /// </summary>
    [HttpGet("{platform}")]
    public async Task<ActionResult<PlatformSettingsDto>> GetByPlatform(GitPlatformType platform, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetByPlatformAsync(platform, cancellationToken);
        if (settings == null)
            return NotFound();

        return Ok(MapToDto(settings));
    }

    /// <summary>
    /// 儲存平台設定（新增或更新）
    /// </summary>
    [HttpPut("{platform}")]
    public async Task<ActionResult<PlatformSettingsDto>> Upsert(
        GitPlatformType platform,
        [FromBody] UpsertPlatformSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = new PlatformSettings
        {
            Platform = platform,
            AccessToken = request.AccessToken,
            WebhookSecret = request.WebhookSecret,
            ApiBaseUrl = request.ApiBaseUrl
        };

        var result = await _settingsRepository.UpsertAsync(settings, cancellationToken);
        _logger.LogInformation("Upserted platform settings for {Platform}", platform);

        return Ok(MapToDto(result));
    }

    private static PlatformSettingsDto MapToDto(PlatformSettings settings)
    {
        return new PlatformSettingsDto
        {
            Id = settings.Id,
            Platform = (int)settings.Platform,
            PlatformName = settings.Platform.ToString(),
            HasAccessToken = !string.IsNullOrEmpty(settings.AccessToken),
            HasWebhookSecret = !string.IsNullOrEmpty(settings.WebhookSecret),
            ApiBaseUrl = settings.ApiBaseUrl,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }
}

public class PlatformSettingsDto
{
    public Guid Id { get; set; }
    public int Platform { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public bool HasAccessToken { get; set; }
    public bool HasWebhookSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpsertPlatformSettingsRequest
{
    public required string AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
}
