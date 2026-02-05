using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Application.Services;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IGitPlatformServiceFactory _gitServiceFactory;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(
        IRepositoryRepository repositoryRepository,
        IPlatformSettingsRepository platformSettingsRepository,
        IGitPlatformServiceFactory gitServiceFactory,
        ILogger<RepositoriesController> logger)
    {
        _repositoryRepository = repositoryRepository;
        _platformSettingsRepository = platformSettingsRepository;
        _gitServiceFactory = gitServiceFactory;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有啟用的 Repository
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepositoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var repositories = await _repositoryRepository.GetAllActiveAsync(cancellationToken);
        return Ok(repositories.Select(MapToDto));
    }

    /// <summary>
    /// 取得單一 Repository
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RepositoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var repository = await _repositoryRepository.GetByIdAsync(id, cancellationToken);
        if (repository == null)
            return NotFound();

        return Ok(MapToDto(repository));
    }

    /// <summary>
    /// 新增 Repository（自動從平台取得 Repository 資訊）
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RepositoryDto>> Create([FromBody] CreateRepositoryRequest request, CancellationToken cancellationToken)
    {
        // 取得平台設定
        var platformSettings = await _platformSettingsRepository.GetByPlatformAsync(request.Platform, cancellationToken);
        if (platformSettings == null || string.IsNullOrEmpty(platformSettings.AccessToken))
        {
            return BadRequest($"請先設定 {request.Platform} 平台的 Access Token");
        }

        // 從平台 API 取得 Repository 資訊
        var gitService = _gitServiceFactory.GetService(request.Platform);
        RepositoryInfo? repoInfo = null;
        try
        {
            repoInfo = await gitService.GetRepositoryInfoAsync(
                request.FullName,
                platformSettings.AccessToken,
                platformSettings.ApiBaseUrl,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch repository info for {FullName}", request.FullName);
        }

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = repoInfo?.Name ?? request.FullName.Split('/').LastOrDefault() ?? request.FullName,
            FullName = request.FullName,
            Platform = request.Platform,
            PlatformRepositoryId = repoInfo?.Id ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repositoryRepository.AddAsync(repository, cancellationToken);
        _logger.LogInformation("Created repository {RepositoryName} ({Platform})", repository.FullName, repository.Platform);

        return CreatedAtAction(nameof(GetById), new { id = repository.Id }, MapToDto(repository));
    }

    /// <summary>
    /// 從平台查詢 Repository 資訊
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<RepositoryLookupResult>> LookupRepository(
        [FromQuery] GitPlatformType platform,
        [FromQuery] string fullName,
        CancellationToken cancellationToken)
    {
        var platformSettings = await _platformSettingsRepository.GetByPlatformAsync(platform, cancellationToken);
        if (platformSettings == null || string.IsNullOrEmpty(platformSettings.AccessToken))
        {
            return BadRequest($"請先設定 {platform} 平台的 Access Token");
        }

        try
        {
            var gitService = _gitServiceFactory.GetService(platform);
            var repoInfo = await gitService.GetRepositoryInfoAsync(
                fullName,
                platformSettings.AccessToken,
                platformSettings.ApiBaseUrl,
                cancellationToken);

            if (repoInfo == null)
            {
                return NotFound($"找不到 Repository: {fullName}");
            }

            return Ok(new RepositoryLookupResult
            {
                Id = repoInfo.Id,
                Name = repoInfo.Name,
                FullName = repoInfo.FullName,
                Description = repoInfo.Description,
                DefaultBranch = repoInfo.DefaultBranch,
                Private = repoInfo.Private
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup repository {FullName} on {Platform}", fullName, platform);
            return BadRequest($"查詢失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新 Repository
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRepositoryRequest request, CancellationToken cancellationToken)
    {
        var repository = await _repositoryRepository.GetByIdAsync(id, cancellationToken);
        if (repository == null)
            return NotFound();

        repository.Name = request.Name ?? repository.Name;
        repository.IsActive = request.IsActive ?? repository.IsActive;
        repository.UpdatedAt = DateTime.UtcNow;

        await _repositoryRepository.UpdateAsync(repository, cancellationToken);
        _logger.LogInformation("Updated repository {RepositoryId}", id);

        return NoContent();
    }

    /// <summary>
    /// 刪除 Repository
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _repositoryRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted repository {RepositoryId}", id);
        return NoContent();
    }

    private static RepositoryDto MapToDto(Repository repository)
    {
        return new RepositoryDto
        {
            Id = repository.Id,
            Name = repository.Name,
            FullName = repository.FullName,
            Platform = repository.Platform.ToString(),
            PlatformRepositoryId = repository.PlatformRepositoryId,
            IsActive = repository.IsActive,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
            RuleCount = repository.RuleMappings?.Count(m => m.IsActive) ?? 0
        };
    }
}

public class RepositoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string PlatformRepositoryId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RuleCount { get; set; }
}

public class CreateRepositoryRequest
{
    /// <summary>
    /// Repository 完整名稱 (owner/repo)
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Git 平台類型
    /// </summary>
    public GitPlatformType Platform { get; set; }
}

public class UpdateRepositoryRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

public class RepositoryLookupResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultBranch { get; set; }
    public bool Private { get; set; }
}
