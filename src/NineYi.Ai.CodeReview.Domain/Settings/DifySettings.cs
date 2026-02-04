namespace NineYi.Ai.CodeReview.Domain.Settings;

/// <summary>
/// Dify API 全域設定
/// </summary>
public class DifySettings
{
    public const string SectionName = "Dify";

    /// <summary>
    /// Dify API Endpoint（預設）
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://api.dify.ai/v1/workflows/run";

    /// <summary>
    /// 每 1000 tokens 的費用（USD）
    /// </summary>
    public decimal CostPer1000Tokens { get; set; } = 0.002m;
}
