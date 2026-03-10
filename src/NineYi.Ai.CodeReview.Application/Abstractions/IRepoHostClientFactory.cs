using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Application.Abstractions;

/// <summary>
/// 根據 Git 平台類型，回傳對應的 <see cref="IRepoHostClient"/> 實作。
/// </summary>
public interface IRepoHostClientFactory
{
    /// <summary>
    /// 取得指定平台的 client 實作。
    /// </summary>
    /// <param name="platform">目標平台（GitHub / GitLab / Bitbucket）。</param>
    /// <returns>對應平台的 <see cref="IRepoHostClient"/>。</returns>
    /// <exception cref="ArgumentOutOfRangeException">傳入不支援的平台時擲出。</exception>
    IRepoHostClient GetClient(GitPlatformType platform);
}
