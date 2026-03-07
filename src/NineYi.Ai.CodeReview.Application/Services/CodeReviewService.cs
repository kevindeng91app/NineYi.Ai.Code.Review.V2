using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineYi.Ai.CodeReview.Application.Commands;
using NineYi.Ai.CodeReview.Application.DTOs;
using NineYi.Ai.CodeReview.Application.Options;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Domain.Settings;

namespace NineYi.Ai.CodeReview.Application.Services;

public class CodeReviewService : ICodeReviewService
{
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IReviewLogRepository _reviewLogRepository;
    private readonly IHotKeywordRepository _hotKeywordRepository;
    private readonly IRuleStatisticsRepository _ruleStatisticsRepository;
    private readonly IDifyUsageLogRepository _difyUsageLogRepository;
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IGitPlatformServiceFactory _gitPlatformServiceFactory;
    private readonly IDifyService _difyService;
    private readonly DifySettings _difySettings;
    private readonly PullRequestIgnoreOptions _ignoreOptions;
    private readonly ILogger<CodeReviewService> _logger;

    public CodeReviewService(
        IRepositoryRepository repositoryRepository,
        IRuleRepository ruleRepository,
        IReviewLogRepository reviewLogRepository,
        IHotKeywordRepository hotKeywordRepository,
        IRuleStatisticsRepository ruleStatisticsRepository,
        IDifyUsageLogRepository difyUsageLogRepository,
        IPlatformSettingsRepository platformSettingsRepository,
        IGitPlatformServiceFactory gitPlatformServiceFactory,
        IDifyService difyService,
        IOptions<DifySettings> difySettings,
        IOptions<PullRequestIgnoreOptions> ignoreOptions,
        ILogger<CodeReviewService> logger)
    {
        _repositoryRepository = repositoryRepository;
        _ruleRepository = ruleRepository;
        _reviewLogRepository = reviewLogRepository;
        _hotKeywordRepository = hotKeywordRepository;
        _ruleStatisticsRepository = ruleStatisticsRepository;
        _difyUsageLogRepository = difyUsageLogRepository;
        _platformSettingsRepository = platformSettingsRepository;
        _gitPlatformServiceFactory = gitPlatformServiceFactory;
        _difyService = difyService;
        _difySettings = difySettings.Value;
        _ignoreOptions = ignoreOptions.Value;
        _logger = logger;
    }

    public async Task<ReviewResultDto> StartAsync(StartCodeReviewCommand command, CancellationToken cancellationToken = default)
    {
        // PR Title Ignore Gate：命中 keyword 直接 log + 回傳，不寫 DB、不呼叫 Dify
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

        // Step 2：橋接到現有 ProcessPullRequestAsync（Phase 2 移除）
        var payload = new WebhookPayload
        {
            Platform = command.ProviderType,
            Repository = new WebhookRepository
            {
                FullName = command.RepoFullName
            },
            PullRequest = new WebhookPullRequest
            {
                Number = command.PullRequestNumber,
                Title = command.Title,
                SourceBranch = command.PullRequestRef.SourceBranch,
                TargetBranch = command.PullRequestRef.TargetBranch,
                HeadSha = command.PullRequestRef.HeadCommitSha
            }
        };

        return await ProcessPullRequestAsync(payload, cancellationToken);
    }

    public async Task<ReviewResultDto> ProcessPullRequestAsync(WebhookPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload.PullRequest == null)
        {
            throw new ArgumentException("Pull request information is required", nameof(payload));
        }

        _logger.LogInformation("Processing PR #{PrNumber} for repository {Repository}",
            payload.PullRequest.Number, payload.Repository.FullName);

        // 1. 取得 Repository 設定
        var repository = await _repositoryRepository.GetByPlatformIdAsync(
            payload.Platform, payload.Repository.Id, cancellationToken);

        if (repository == null)
        {
            repository = await _repositoryRepository.GetByFullNameAsync(
                payload.Platform, payload.Repository.FullName, cancellationToken);
        }

        if (repository == null || !repository.IsActive)
        {
            _logger.LogWarning("Repository {Repository} not found or inactive", payload.Repository.FullName);
            return new ReviewResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Repository not configured or inactive"
            };
        }

        // 2. 建立 Review Log
        var reviewLog = new ReviewLog
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            PullRequestNumber = payload.PullRequest.Number,
            PullRequestTitle = payload.PullRequest.Title,
            Author = payload.PullRequest.Author?.Username ?? "unknown",
            Status = ReviewStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };
        await _reviewLogRepository.AddAsync(reviewLog, cancellationToken);

        try
        {
            // 3. 取得平台設定
            var platformSettings = await _platformSettingsRepository.GetByPlatformAsync(payload.Platform, cancellationToken);
            if (platformSettings == null || string.IsNullOrEmpty(platformSettings.AccessToken))
            {
                throw new InvalidOperationException($"Platform settings not configured for {payload.Platform}");
            }

            // 4. 取得 Git 平台服務
            var gitService = _gitPlatformServiceFactory.GetService(payload.Platform);

            // 5. 取得 PR 的檔案清單
            var files = await gitService.GetPullRequestFilesAsync(
                repository.FullName, payload.PullRequest.Number,
                platformSettings.AccessToken, platformSettings.ApiBaseUrl, cancellationToken);

            // 6. 取得 Repository 對應的 Rules
            var rules = (await _ruleRepository.GetByRepositoryIdAsync(repository.Id, cancellationToken)).ToList();
            if (!rules.Any())
            {
                _logger.LogWarning("No rules configured for repository {Repository}", repository.FullName);
            }

            // 7. 取得 Hot Keywords
            var hotKeywords = (await _hotKeywordRepository.GetAllActiveAsync(cancellationToken)).ToList();

            // 8. 處理每個檔案
            var result = new ReviewResultDto
            {
                ReviewLogId = reviewLog.Id,
                IsSuccess = true
            };

            foreach (var file in files)
            {
                if (file.ChangeType == FileChangeType.Deleted)
                {
                    continue; // 跳過已刪除的檔案
                }

                var fileResult = await ProcessFileAsync(
                    repository, payload.PullRequest.Number, file,
                    rules, hotKeywords, gitService, reviewLog.Id, cancellationToken);

                result.FileResults.Add(fileResult);
                result.FilesProcessed++;

                if (fileResult.HasComments)
                {
                    result.CommentsGenerated += fileResult.Comments.Count;

                    // 發送評論到 Git 平台
                    foreach (var comment in fileResult.Comments)
                    {
                        await gitService.PostReviewCommentAsync(
                            repository.FullName, payload.PullRequest.Number,
                            file.FileName, comment.LineNumber ?? 1,
                            FormatComment(comment), platformSettings.AccessToken,
                            platformSettings.ApiBaseUrl, cancellationToken);
                    }
                }
            }

            // 9. 如果所有檔案都沒問題，在 PR 發表整體評論
            if (result.CommentsGenerated == 0)
            {
                await gitService.PostPullRequestCommentAsync(
                    repository.FullName, payload.PullRequest.Number,
                    "✅ Code review completed. No issues found. Great job!",
                    platformSettings.AccessToken, platformSettings.ApiBaseUrl, cancellationToken);
            }

            // 10. 更新 Review Log
            reviewLog.Status = ReviewStatus.Completed;
            reviewLog.CompletedAt = DateTime.UtcNow;
            reviewLog.FilesProcessed = result.FilesProcessed;
            reviewLog.CommentsGenerated = result.CommentsGenerated;
            reviewLog.TokensConsumed = result.TotalTokensConsumed;
            reviewLog.EstimatedCost = result.EstimatedCost;
            await _reviewLogRepository.UpdateAsync(reviewLog, cancellationToken);

            _logger.LogInformation("PR #{PrNumber} review completed. Files: {Files}, Comments: {Comments}",
                payload.PullRequest.Number, result.FilesProcessed, result.CommentsGenerated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PR #{PrNumber}", payload.PullRequest.Number);

            reviewLog.Status = ReviewStatus.Failed;
            reviewLog.CompletedAt = DateTime.UtcNow;
            reviewLog.ErrorMessage = ex.Message;
            await _reviewLogRepository.UpdateAsync(reviewLog, cancellationToken);

            return new ReviewResultDto
            {
                ReviewLogId = reviewLog.Id,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<FileReviewResultDto> ProcessFileAsync(
        Repository repository,
        int prNumber,
        PullRequestFile file,
        List<Rule> rules,
        List<HotKeyword> hotKeywords,
        IGitPlatformService gitService,
        Guid reviewLogId,
        CancellationToken cancellationToken)
    {
        var result = new FileReviewResultDto
        {
            FilePath = file.FileName
        };

        // 1. 檢查 Hot Keywords
        if (file.Patch != null)
        {
            foreach (var keyword in hotKeywords)
            {
                if (!IsFilePatternMatch(file.FileName, keyword.FilePatterns))
                    continue;

                bool isMatch = keyword.IsRegex
                    ? Regex.IsMatch(file.Patch, keyword.Keyword, RegexOptions.IgnoreCase)
                    : file.Patch.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase);

                if (isMatch)
                {
                    result.MatchedKeywords.Add(keyword.Keyword);
                    result.Comments.Add(new CommentDto
                    {
                        Comment = $"⚠️ **{keyword.Category} Alert**: {keyword.AlertMessage}",
                        Severity = keyword.Severity.ToString().ToLower(),
                        Category = keyword.Category.ToString()
                    });

                    // 更新關鍵字觸發次數
                    await _hotKeywordRepository.IncrementTriggerCountAsync(keyword.Id, cancellationToken);
                }
            }
        }

        // 2. 對每個適用的 Rule 進行 Code Review
        foreach (var rule in rules.Where(r => r.IsActive && IsFilePatternMatch(file.FileName, r.FilePatterns)))
        {
            var reviewRequest = new DifyReviewRequest
            {
                ApiEndpoint = rule.DifyApiEndpoint,
                ApiKey = rule.DifyApiKey,
                FileName = file.FileName,
                FileDiff = file.Patch ?? string.Empty
            };

            var reviewResult = await _difyService.ReviewCodeAsync(reviewRequest, cancellationToken);

            // 記錄 Dify 使用量
            await _difyUsageLogRepository.AddAsync(new DifyUsageLog
            {
                Id = Guid.NewGuid(),
                ReviewLogId = reviewLogId,
                RuleId = rule.Id,
                DifyRequestId = reviewResult.RequestId,
                InputTokens = reviewResult.InputTokens,
                OutputTokens = reviewResult.OutputTokens,
                TotalTokens = reviewResult.TotalTokens,
                EstimatedCost = CalculateCost(reviewResult.TotalTokens),
                ModelName = reviewResult.ModelName,
                DurationMs = reviewResult.DurationMs,
                IsSuccess = reviewResult.IsSuccess,
                ErrorMessage = reviewResult.ErrorMessage
            }, cancellationToken);

            // 更新規則統計
            await _ruleStatisticsRepository.IncrementTriggerAsync(
                rule.Id, DateOnly.FromDateTime(DateTime.UtcNow),
                reviewResult.TotalTokens, reviewResult.HasIssues, cancellationToken);

            if (reviewResult.IsSuccess && reviewResult.HasIssues)
            {
                foreach (var comment in reviewResult.Comments)
                {
                    result.Comments.Add(new CommentDto
                    {
                        LineNumber = comment.LineNumber,
                        Comment = comment.Comment,
                        Severity = comment.Severity,
                        Category = comment.Category,
                        RuleName = rule.Name
                    });
                }
            }
        }

        result.HasComments = result.Comments.Any();

        // 記錄檔案 Log
        await _reviewLogRepository.AddFileLogAsync(new ReviewFileLog
        {
            Id = Guid.NewGuid(),
            ReviewLogId = reviewLogId,
            FilePath = file.FileName,
            ChangeType = file.ChangeType,
            LinesAdded = file.Additions,
            LinesDeleted = file.Deletions,
            HasComments = result.HasComments,
            Comments = result.HasComments ? JsonSerializer.Serialize(result.Comments) : null,
            MatchedKeywords = result.MatchedKeywords.Any() ? string.Join(",", result.MatchedKeywords) : null
        }, cancellationToken);

        return result;
    }

    private static bool IsFilePatternMatch(string fileName, string? patterns)
    {
        if (string.IsNullOrWhiteSpace(patterns))
            return true; // 沒有指定 pattern 則全部適用

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

    private static string FormatComment(CommentDto comment)
    {
        var severity = comment.Severity.ToUpper() switch
        {
            "ERROR" => "🔴",
            "WARNING" => "🟡",
            _ => "🔵"
        };

        var category = string.IsNullOrEmpty(comment.Category) ? "" : $"[{comment.Category}] ";
        var ruleName = string.IsNullOrEmpty(comment.RuleName) ? "" : $"(Rule: {comment.RuleName})";

        return $"{severity} {category}{comment.Comment} {ruleName}".Trim();
    }

    private decimal CalculateCost(int tokens)
    {
        return tokens * _difySettings.CostPer1000Tokens / 1000;
    }

    public async Task<ReviewResultDto?> GetReviewStatusAsync(Guid reviewLogId, CancellationToken cancellationToken = default)
    {
        var reviewLog = await _reviewLogRepository.GetByIdAsync(reviewLogId, cancellationToken);
        if (reviewLog == null)
            return null;

        return new ReviewResultDto
        {
            ReviewLogId = reviewLog.Id,
            IsSuccess = reviewLog.Status == ReviewStatus.Completed,
            FilesProcessed = reviewLog.FilesProcessed,
            CommentsGenerated = reviewLog.CommentsGenerated,
            TotalTokensConsumed = reviewLog.TokensConsumed,
            EstimatedCost = reviewLog.EstimatedCost,
            ErrorMessage = reviewLog.ErrorMessage
        };
    }
}
