using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;

namespace NineYi.Ai.CodeReview.Application.Services;

public interface IGitPlatformServiceFactory
{
    IGitPlatformService GetService(GitPlatformType platform);
}
