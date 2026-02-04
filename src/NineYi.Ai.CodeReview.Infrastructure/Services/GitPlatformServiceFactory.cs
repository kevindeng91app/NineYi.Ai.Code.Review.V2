using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Infrastructure.Services;

public class GitPlatformServiceFactory : IGitPlatformServiceFactory
{
    private readonly IEnumerable<IGitPlatformService> _services;

    public GitPlatformServiceFactory(IEnumerable<IGitPlatformService> services)
    {
        _services = services;
    }

    public IGitPlatformService GetService(GitPlatformType platform)
    {
        var service = _services.FirstOrDefault(s => s.Platform == platform);
        if (service == null)
        {
            throw new NotSupportedException($"Git platform {platform} is not supported");
        }
        return service;
    }
}
