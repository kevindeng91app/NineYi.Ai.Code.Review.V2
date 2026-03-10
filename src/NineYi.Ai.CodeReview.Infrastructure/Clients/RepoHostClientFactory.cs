using NineYi.Ai.CodeReview.Application.Abstractions;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Infrastructure.Clients;

/// <summary>
/// 根據 <see cref="GitPlatformType"/> 回傳對應的 <see cref="IRepoHostClient"/> 實作。
/// 這是整個系統中唯一需要判斷「現在是哪個平台」的地方。
/// </summary>
public class RepoHostClientFactory : IRepoHostClientFactory
{
    private readonly GitHubClient _gitHub;
    private readonly GitLabClient _gitLab;
    private readonly BitbucketClient _bitbucket;

    public RepoHostClientFactory(
        GitHubClient gitHub,
        GitLabClient gitLab,
        BitbucketClient bitbucket)
    {
        _gitHub = gitHub;
        _gitLab = gitLab;
        _bitbucket = bitbucket;
    }

    /// <inheritdoc />
    public IRepoHostClient GetClient(GitPlatformType platform) => platform switch
    {
        GitPlatformType.GitHub    => _gitHub,
        GitPlatformType.GitLab    => _gitLab,
        GitPlatformType.Bitbucket => _bitbucket,
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, $"Unsupported platform: {platform}")
    };
}
