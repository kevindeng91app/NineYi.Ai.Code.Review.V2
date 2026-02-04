using Microsoft.AspNetCore.Mvc;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotKeywordsController : ControllerBase
{
    private readonly IHotKeywordRepository _hotKeywordRepository;
    private readonly ILogger<HotKeywordsController> _logger;

    public HotKeywordsController(
        IHotKeywordRepository hotKeywordRepository,
        ILogger<HotKeywordsController> logger)
    {
        _hotKeywordRepository = hotKeywordRepository;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有啟用的 Hot Keywords
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HotKeywordDto>>> GetAll(CancellationToken cancellationToken)
    {
        var keywords = await _hotKeywordRepository.GetAllActiveAsync(cancellationToken);
        return Ok(keywords.Select(MapToDto));
    }

    /// <summary>
    /// 依類別取得 Hot Keywords
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<HotKeywordDto>>> GetByCategory(KeywordCategory category, CancellationToken cancellationToken)
    {
        var keywords = await _hotKeywordRepository.GetByCategoryAsync(category, cancellationToken);
        return Ok(keywords.Select(MapToDto));
    }

    /// <summary>
    /// 取得單一 Hot Keyword
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HotKeywordDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var keyword = await _hotKeywordRepository.GetByIdAsync(id, cancellationToken);
        if (keyword == null)
            return NotFound();

        return Ok(MapToDto(keyword));
    }

    /// <summary>
    /// 新增 Hot Keyword
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HotKeywordDto>> Create([FromBody] CreateHotKeywordRequest request, CancellationToken cancellationToken)
    {
        var keyword = new HotKeyword
        {
            Id = Guid.NewGuid(),
            Keyword = request.Keyword,
            Category = request.Category,
            Severity = request.Severity,
            AlertMessage = request.AlertMessage,
            IsRegex = request.IsRegex,
            FilePatterns = request.FilePatterns,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _hotKeywordRepository.AddAsync(keyword, cancellationToken);
        _logger.LogInformation("Created hot keyword {Keyword} ({Category})", keyword.Keyword, keyword.Category);

        return CreatedAtAction(nameof(GetById), new { id = keyword.Id }, MapToDto(keyword));
    }

    /// <summary>
    /// 更新 Hot Keyword
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHotKeywordRequest request, CancellationToken cancellationToken)
    {
        var keyword = await _hotKeywordRepository.GetByIdAsync(id, cancellationToken);
        if (keyword == null)
            return NotFound();

        keyword.Keyword = request.Keyword ?? keyword.Keyword;
        keyword.Category = request.Category ?? keyword.Category;
        keyword.Severity = request.Severity ?? keyword.Severity;
        keyword.AlertMessage = request.AlertMessage ?? keyword.AlertMessage;
        keyword.IsRegex = request.IsRegex ?? keyword.IsRegex;
        keyword.FilePatterns = request.FilePatterns ?? keyword.FilePatterns;
        keyword.IsActive = request.IsActive ?? keyword.IsActive;

        await _hotKeywordRepository.UpdateAsync(keyword, cancellationToken);
        _logger.LogInformation("Updated hot keyword {KeywordId}", id);

        return NoContent();
    }

    /// <summary>
    /// 刪除 Hot Keyword
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _hotKeywordRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted hot keyword {KeywordId}", id);
        return NoContent();
    }

    private static HotKeywordDto MapToDto(HotKeyword keyword)
    {
        return new HotKeywordDto
        {
            Id = keyword.Id,
            Keyword = keyword.Keyword,
            Category = keyword.Category.ToString(),
            Severity = keyword.Severity.ToString(),
            AlertMessage = keyword.AlertMessage,
            IsRegex = keyword.IsRegex,
            FilePatterns = keyword.FilePatterns,
            TriggerCount = keyword.TriggerCount,
            IsActive = keyword.IsActive,
            CreatedAt = keyword.CreatedAt,
            UpdatedAt = keyword.UpdatedAt
        };
    }
}

public class HotKeywordDto
{
    public Guid Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string AlertMessage { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string? FilePatterns { get; set; }
    public int TriggerCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateHotKeywordRequest
{
    public required string Keyword { get; set; }
    public KeywordCategory Category { get; set; }
    public SeverityLevel Severity { get; set; }
    public required string AlertMessage { get; set; }
    public bool IsRegex { get; set; }
    public string? FilePatterns { get; set; }
}

public class UpdateHotKeywordRequest
{
    public string? Keyword { get; set; }
    public KeywordCategory? Category { get; set; }
    public SeverityLevel? Severity { get; set; }
    public string? AlertMessage { get; set; }
    public bool? IsRegex { get; set; }
    public string? FilePatterns { get; set; }
    public bool? IsActive { get; set; }
}
