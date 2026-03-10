using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineYi.Ai.CodeReview.Application.Abstractions;
using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.DTOs;
using NineYi.Ai.CodeReview.Application.Options;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Application.Services;

public class CodeReviewService : ICodeReviewService
{
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IDifyService _difyService;
    private readonly IRepoHostClientFactory _repoHostClientFactory;
    private readonly PullRequestIgnoreOptions _ignoreOptions;
    private readonly ILogger<CodeReviewService> _logger;

    public CodeReviewService(
        IRepositoryRepository repositoryRepository,
        IRuleRepository ruleRepository,
        IDifyService difyService,
        IRepoHostClientFactory repoHostClientFactory,
        IOptions<PullRequestIgnoreOptions> ignoreOptions,
        ILogger<CodeReviewService> logger)
    {
        _repositoryRepository = repositoryRepository;
        _ruleRepository = ruleRepository;
        _difyService = difyService;
        _repoHostClientFactory = repoHostClientFactory;
        _ignoreOptions = ignoreOptions.Value;
        _logger = logger;
    }

    public async Task<ReviewResultDto> StartAsync(StartCodeReviewCommand command, CancellationToken cancellationToken = default)
    {
        // Step 1: PR Title Ignore Gate
        var matchedKeyword = _ignoreOptions.TitleKeywords
            .FirstOrDefault(k => command.Title.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (matchedKeyword is not null)
        {
            _logger.LogInformation(
                "PR #{Number} in {Repo} skipped — title matched ignore keyword '{Keyword}'. Title: {Title}",
                command.PullRequestNumber, command.RepoFullName, matchedKeyword, command.Title);

            return new ReviewResultDto
            {
                IsSuccess = true,
                ErrorMessage = $"Skipped: PR title matched ignore keyword '{matchedKeyword}'"
            };
        }

        // Step 2: Repo 白名單驗證（DB READ）
        var repository = await _repositoryRepository.GetByFullNameAsync(
            command.ProviderType, command.RepoFullName, cancellationToken);

        if (repository == null || !repository.IsActive)
        {
            _logger.LogWarning("Repository {Repository} not found or inactive", command.RepoFullName);
            return new ReviewResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Repository not configured or inactive"
            };
        }

        // Step 3: 取得對應平台的 IRepoHostClient
        var client = _repoHostClientFactory.GetClient(command.ProviderType);

        // Step 4: 取得 diff 檔案清單
        var diffFiles = await client.GetPullRequestDiffFilesAsync(command, cancellationToken);

        _logger.LogInformation(
            "PR #{Number} in {Repo}: retrieved {Count} diff files",
            command.PullRequestNumber, command.RepoFullName, diffFiles.Count);

        // Step 5: 取得適用的 Rules（DB READ）
        var rules = (await _ruleRepository.GetByRepositoryIdAsync(repository.Id, cancellationToken)).ToList();

        if (!rules.Any())
        {
            _logger.LogWarning(
                "PR #{Number} in {Repo}: no rules configured, posting LGTM comment",
                command.PullRequestNumber, command.RepoFullName);
        }

        // Step 6: 逐檔 × 逐 Rule 呼叫 Dify
        var allFindings = new List<(string FilePath, CodeReviewComment Comment, string RuleName)>();

        foreach (var diffFile in diffFiles)
        {
            var applicableRules = rules
                .Where(r => r.IsActive && IsFilePatternMatch(diffFile.FilePath, r.FilePatterns))
                .ToList();

            foreach (var rule in applicableRules)
            {
                var result = await _difyService.ReviewCodeAsync(new DifyReviewRequest
                {
                    ApiKey   = rule.DifyApiKey,
                    FileName = diffFile.FilePath,
                    FileDiff = diffFile.Diff
                }, cancellationToken);

                _logger.LogInformation(
                    "Dify result: pr={Pr} file={File} rule={Rule} success={Success} findings={Findings} tokens={Tokens} durationMs={Duration}",
                    command.PullRequestNumber, diffFile.FilePath, rule.Name,
                    result.IsSuccess, result.Comments.Count, result.TotalTokens, result.DurationMs);

                if (result.IsSuccess && result.HasIssues)
                {
                    foreach (var comment in result.Comments)
                        allFindings.Add((diffFile.FilePath, comment, rule.Name));
                }
            }
        }

        // Step 7: Post comment 回 Git 平台
        if (allFindings.Any())
        {
            foreach (var (filePath, comment, ruleName) in allFindings)
            {
                if (comment.StartLine.HasValue)
                {
                    // 行號 parse 成功 → inline comment
                    await client.PostReviewCommentAsync(
                        command,
                        filePath,
                        comment.StartLine.Value,
                        comment.EndLine ?? comment.StartLine.Value,
                        FormatFinding(comment, ruleName),
                        cancellationToken);
                }
                else
                {
                    // 行號 parse 失敗 → fallback 到 PR 層級
                    await client.PostPullRequestCommentAsync(
                        command,
                        $"📁 `{filePath}`\n\n{FormatFinding(comment, ruleName)}",
                        cancellationToken);
                }
            }
        }
        else
        {
            await client.PostPullRequestCommentAsync(
                command,
                "✅ AI Code Review completed. No issues found. Great job!",
                cancellationToken);
        }

        // Step 8: 回傳結果
        return new ReviewResultDto
        {
            IsSuccess         = true,
            FilesProcessed    = diffFiles.Count,
            CommentsGenerated = allFindings.Count
        };
    }

    /// <summary>
    /// 格式化單一 finding 為評論字串。
    /// 例：🟡 [Performance] 建議說明 (Rule: PerformanceRule)
    /// </summary>
    private static string FormatFinding(CodeReviewComment comment, string ruleName)
    {
        var icon = comment.Severity.ToUpper() switch
        {
            "ERROR"   => "🔴",
            "WARNING" => "🟡",
            _         => "🔵"
        };
        var category = string.IsNullOrEmpty(comment.Category) ? "" : $"[{comment.Category}] ";
        return $"{icon} {category}{comment.Comment} (Rule: {ruleName})".Trim();
    }

    /// <summary>
    /// 判斷檔案名稱是否符合逗號分隔的 glob 樣式（* 萬用字元）。
    /// patterns 為 null 或空字串時，視為全部適用。
    /// </summary>
    private static bool IsFilePatternMatch(string fileName, string? patterns)
    {
        if (string.IsNullOrWhiteSpace(patterns))
            return true;

        var patternList = patterns.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pattern in patternList)
        {
            var regexPattern = "^" + Regex.Escape(pattern.Trim())
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }
}
