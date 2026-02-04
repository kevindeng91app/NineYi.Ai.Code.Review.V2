using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Domain.Settings;

namespace NineYi.Ai.CodeReview.Infrastructure.Services;

public class DifyService : IDifyService
{
    private readonly HttpClient _httpClient;
    private readonly DifySettings _settings;
    private readonly ILogger<DifyService> _logger;

    public DifyService(HttpClient httpClient, IOptions<DifySettings> settings, ILogger<DifyService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DifyReviewResult> ReviewCodeAsync(DifyReviewRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 使用 request 中的 endpoint，若未設定則使用全域設定
        var apiEndpoint = !string.IsNullOrWhiteSpace(request.ApiEndpoint)
            ? request.ApiEndpoint
            : _settings.ApiEndpoint;

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

            var payload = new
            {
                inputs = new
                {
                    file_name = request.FileName,
                    file_diff = request.FileDiff,
                    file_content = request.FileContent ?? string.Empty,
                    additional_context = request.AdditionalContext ?? string.Empty
                },
                response_mode = "blocking",
                user = "code-review-bot"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiEndpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Dify API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new DifyReviewResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Dify API error: {response.StatusCode}",
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    RawResponse = responseContent
                };
            }

            var difyResponse = JsonSerializer.Deserialize<DifyApiResponse>(responseContent, JsonOptions);

            if (difyResponse == null)
            {
                return new DifyReviewResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to parse Dify response",
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    RawResponse = responseContent
                };
            }

            // 解析 Dify 回應中的 code review 結果
            var comments = ParseReviewComments(difyResponse.Answer);

            return new DifyReviewResult
            {
                IsSuccess = true,
                RequestId = difyResponse.MessageId,
                HasIssues = comments.Any(),
                Comments = comments,
                InputTokens = difyResponse.Metadata?.Usage?.PromptTokens ?? 0,
                OutputTokens = difyResponse.Metadata?.Usage?.CompletionTokens ?? 0,
                TotalTokens = difyResponse.Metadata?.Usage?.TotalTokens ?? 0,
                ModelName = difyResponse.Metadata?.Usage?.Model,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error calling Dify API");
            return new DifyReviewResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<DifyUsageInfo> GetUsageInfoAsync(string apiKey, string endpoint, CancellationToken cancellationToken = default)
    {
        // Dify 目前沒有直接的 usage API，這裡返回基本資訊
        // 實際的使用量資訊需要從我們自己的 DifyUsageLog 資料表取得
        return new DifyUsageInfo
        {
            TotalCost = 0,
            TotalTokens = 0,
            TotalRequests = 0
        };
    }

    private List<CodeReviewComment> ParseReviewComments(string? answer)
    {
        var comments = new List<CodeReviewComment>();

        if (string.IsNullOrWhiteSpace(answer))
            return comments;

        // 嘗試解析 JSON 格式的回應
        try
        {
            // 先嘗試解析是否為純 JSON
            if (answer.TrimStart().StartsWith("[") || answer.TrimStart().StartsWith("{"))
            {
                var jsonComments = JsonSerializer.Deserialize<List<DifyCommentJson>>(answer, JsonOptions);
                if (jsonComments != null)
                {
                    return jsonComments.Select(c => new CodeReviewComment
                    {
                        LineNumber = c.Line,
                        Comment = c.Comment ?? c.Message ?? string.Empty,
                        Severity = c.Severity ?? "info",
                        Category = c.Category,
                        Suggestion = c.Suggestion
                    }).ToList();
                }
            }
        }
        catch
        {
            // JSON 解析失敗，使用文字解析
        }

        // 檢查是否有 "no issues" 或 "looks good" 的回應
        var lowerAnswer = answer.ToLower();
        if (lowerAnswer.Contains("no issues") ||
            lowerAnswer.Contains("looks good") ||
            lowerAnswer.Contains("no problems") ||
            lowerAnswer.Contains("lgtm") ||
            lowerAnswer.Contains("沒有問題") ||
            lowerAnswer.Contains("沒有發現問題"))
        {
            return comments; // 返回空列表
        }

        // 嘗試從文字中提取結構化的評論
        var lines = answer.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        CodeReviewComment? currentComment = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳過空行和標題
            if (string.IsNullOrWhiteSpace(trimmedLine) ||
                trimmedLine.StartsWith("#") ||
                trimmedLine.StartsWith("---"))
                continue;

            // 檢查是否是新的評論項目（以 - 或 * 或數字開頭）
            if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*") ||
                char.IsDigit(trimmedLine[0]))
            {
                if (currentComment != null && !string.IsNullOrWhiteSpace(currentComment.Comment))
                {
                    comments.Add(currentComment);
                }

                currentComment = new CodeReviewComment
                {
                    Comment = trimmedLine.TrimStart('-', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.'),
                    Severity = DetermineSeverity(trimmedLine)
                };

                // 嘗試提取行號
                var lineMatch = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"[Ll]ine\s*(\d+)");
                if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out var lineNum))
                {
                    currentComment.LineNumber = lineNum;
                }
            }
            else if (currentComment != null)
            {
                currentComment.Comment += " " + trimmedLine;
            }
        }

        if (currentComment != null && !string.IsNullOrWhiteSpace(currentComment.Comment))
        {
            comments.Add(currentComment);
        }

        // 如果沒有解析出任何結構化的評論，但有內容，則作為單一評論
        if (!comments.Any() && !string.IsNullOrWhiteSpace(answer) && answer.Length > 20)
        {
            comments.Add(new CodeReviewComment
            {
                Comment = answer,
                Severity = "info"
            });
        }

        return comments;
    }

    private static string DetermineSeverity(string text)
    {
        var lowerText = text.ToLower();
        if (lowerText.Contains("error") || lowerText.Contains("critical") ||
            lowerText.Contains("security") || lowerText.Contains("嚴重"))
            return "error";
        if (lowerText.Contains("warning") || lowerText.Contains("警告") ||
            lowerText.Contains("should") || lowerText.Contains("建議"))
            return "warning";
        return "info";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private class DifyApiResponse
    {
        public string? MessageId { get; set; }
        public string? Answer { get; set; }
        public DifyMetadata? Metadata { get; set; }
    }

    private class DifyMetadata
    {
        public DifyUsage? Usage { get; set; }
    }

    private class DifyUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public string? Model { get; set; }
    }

    private class DifyCommentJson
    {
        public int? Line { get; set; }
        public string? Comment { get; set; }
        public string? Message { get; set; }
        public string? Severity { get; set; }
        public string? Category { get; set; }
        public string? Suggestion { get; set; }
    }
}
