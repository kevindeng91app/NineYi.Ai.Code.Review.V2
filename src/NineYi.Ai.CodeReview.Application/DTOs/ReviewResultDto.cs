namespace NineYi.Ai.CodeReview.Application.DTOs;

public class ReviewResultDto
{
    public Guid ReviewLogId { get; set; }
    public bool IsSuccess { get; set; }
    public int FilesProcessed { get; set; }
    public int CommentsGenerated { get; set; }
    public int TotalTokensConsumed { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FileReviewResultDto> FileResults { get; set; } = new();
}

public class FileReviewResultDto
{
    public string FilePath { get; set; } = string.Empty;
    public bool HasComments { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
    public List<string> MatchedKeywords { get; set; } = new();
}

public class CommentDto
{
    public int? LineNumber { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string? Category { get; set; }
    public string? RuleName { get; set; }
}
