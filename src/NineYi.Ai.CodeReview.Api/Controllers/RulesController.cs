using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IRuleRepository _ruleRepository;
    private readonly CodeReviewDbContext _context;
    private readonly ILogger<RulesController> _logger;

    public RulesController(
        IRuleRepository ruleRepository,
        CodeReviewDbContext context,
        ILogger<RulesController> logger)
    {
        _ruleRepository = ruleRepository;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有啟用的 Rules
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RuleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetAllActiveAsync(cancellationToken);
        return Ok(rules.Select(MapToDto));
    }

    /// <summary>
    /// 取得單一 Rule
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RuleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        return Ok(MapToDto(rule));
    }

    /// <summary>
    /// 取得 Repository 對應的 Rules
    /// </summary>
    [HttpGet("repository/{repositoryId:guid}")]
    public async Task<ActionResult<IEnumerable<RuleDto>>> GetByRepository(Guid repositoryId, CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetByRepositoryIdAsync(repositoryId, cancellationToken);
        return Ok(rules.Select(MapToDto));
    }

    /// <summary>
    /// 新增 Rule
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RuleDto>> Create([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            DifyApiEndpoint = request.DifyApiEndpoint,
            DifyApiKey = request.DifyApiKey,
            Type = request.Type,
            Priority = request.Priority,
            FilePatterns = request.FilePatterns,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _ruleRepository.AddAsync(rule, cancellationToken);
        _logger.LogInformation("Created rule {RuleName} ({RuleType})", rule.Name, rule.Type);

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, MapToDto(rule));
    }

    /// <summary>
    /// 更新 Rule
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        rule.Name = request.Name ?? rule.Name;
        rule.Description = request.Description ?? rule.Description;
        rule.DifyApiEndpoint = request.DifyApiEndpoint ?? rule.DifyApiEndpoint;
        rule.DifyApiKey = request.DifyApiKey ?? rule.DifyApiKey;
        rule.Type = request.Type ?? rule.Type;
        rule.Priority = request.Priority ?? rule.Priority;
        rule.FilePatterns = request.FilePatterns ?? rule.FilePatterns;
        rule.IsActive = request.IsActive ?? rule.IsActive;

        await _ruleRepository.UpdateAsync(rule, cancellationToken);
        _logger.LogInformation("Updated rule {RuleId}", id);

        return NoContent();
    }

    /// <summary>
    /// 刪除 Rule
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _ruleRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted rule {RuleId}", id);
        return NoContent();
    }

    /// <summary>
    /// 將 Rule 關聯到 Repository
    /// </summary>
    [HttpPost("{ruleId:guid}/repositories/{repositoryId:guid}")]
    public async Task<IActionResult> MapToRepository(Guid ruleId, Guid repositoryId, [FromBody] MapRuleRequest? request, CancellationToken cancellationToken)
    {
        var mapping = new RepositoryRuleMapping
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            RuleId = ruleId,
            PriorityOverride = request?.PriorityOverride,
            FilePatternsOverride = request?.FilePatternsOverride,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RepositoryRuleMappings.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mapped rule {RuleId} to repository {RepositoryId}", ruleId, repositoryId);
        return Ok();
    }

    /// <summary>
    /// 取消 Rule 與 Repository 的關聯
    /// </summary>
    [HttpDelete("{ruleId:guid}/repositories/{repositoryId:guid}")]
    public async Task<IActionResult> UnmapFromRepository(Guid ruleId, Guid repositoryId, CancellationToken cancellationToken)
    {
        var mapping = await _context.RepositoryRuleMappings
            .FirstOrDefaultAsync(m => m.RuleId == ruleId && m.RepositoryId == repositoryId, cancellationToken);

        if (mapping != null)
        {
            _context.RepositoryRuleMappings.Remove(mapping);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Unmapped rule {RuleId} from repository {RepositoryId}", ruleId, repositoryId);
        return NoContent();
    }

    private static RuleDto MapToDto(Rule rule)
    {
        return new RuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            Type = rule.Type.ToString(),
            Priority = rule.Priority,
            FilePatterns = rule.FilePatterns,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}

public class RuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? FilePatterns { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRuleRequest
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string DifyApiEndpoint { get; set; }
    public required string DifyApiKey { get; set; }
    public RuleType Type { get; set; }
    public int Priority { get; set; } = 100;
    public string? FilePatterns { get; set; }
}

public class UpdateRuleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DifyApiEndpoint { get; set; }
    public string? DifyApiKey { get; set; }
    public RuleType? Type { get; set; }
    public int? Priority { get; set; }
    public string? FilePatterns { get; set; }
    public bool? IsActive { get; set; }
}

public class MapRuleRequest
{
    public int? PriorityOverride { get; set; }
    public string? FilePatternsOverride { get; set; }
}
