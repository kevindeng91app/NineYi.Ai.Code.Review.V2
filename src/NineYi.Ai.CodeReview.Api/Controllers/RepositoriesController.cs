using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(
        IRepositoryRepository repositoryRepository,
        ILogger<RepositoriesController> logger)
    {
        _repositoryRepository = repositoryRepository;
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
    /// 新增 Repository
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RepositoryDto>> Create([FromBody] CreateRepositoryRequest request, CancellationToken cancellationToken)
    {
        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            FullName = request.FullName,
            Platform = request.Platform,
            PlatformRepositoryId = request.PlatformRepositoryId,
            AccessToken = request.AccessToken,
            WebhookSecret = request.WebhookSecret,
            ApiBaseUrl = request.ApiBaseUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repositoryRepository.AddAsync(repository, cancellationToken);
        _logger.LogInformation("Created repository {RepositoryName} ({Platform})", repository.FullName, repository.Platform);

        return CreatedAtAction(nameof(GetById), new { id = repository.Id }, MapToDto(repository));
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
        repository.FullName = request.FullName ?? repository.FullName;
        repository.AccessToken = request.AccessToken ?? repository.AccessToken;
        repository.WebhookSecret = request.WebhookSecret ?? repository.WebhookSecret;
        repository.ApiBaseUrl = request.ApiBaseUrl ?? repository.ApiBaseUrl;
        repository.IsActive = request.IsActive ?? repository.IsActive;

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
            ApiBaseUrl = repository.ApiBaseUrl,
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
    public string? ApiBaseUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RuleCount { get; set; }
}

public class CreateRepositoryRequest
{
    public required string Name { get; set; }
    public required string FullName { get; set; }
    public GitPlatformType Platform { get; set; }
    public required string PlatformRepositoryId { get; set; }
    public required string AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
}

public class UpdateRepositoryRequest
{
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
    public bool? IsActive { get; set; }
}
