using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace NineYi.Ai.CodeReview.Infrastructure.Http;

/// <summary>
/// 集中管理所有對外 HTTP Client 的 Polly Retry Policy。
/// </summary>
public static class HttpPolicies
{
    /// <summary>
    /// 標準 Retry Policy：
    /// <list type="bullet">
    ///   <item>重試觸發條件：5xx 伺服器錯誤、網路逾時、429 Too Many Requests</item>
    ///   <item>不重試條件：401 / 403 / 404（客戶端錯誤，重試無意義）</item>
    ///   <item>重試 3 次，指數退讓（2s → 4s → 8s）</item>
    /// </list>
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()                                         // 5xx + 網路錯誤
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)     // 429
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
