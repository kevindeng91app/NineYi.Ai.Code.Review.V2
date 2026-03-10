using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NineYi.Ai.CodeReview.Application.Options;
using NineYi.Ai.CodeReview.Application.Services;

namespace NineYi.Ai.CodeReview.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PullRequestIgnoreOptions>(
            configuration.GetSection(PullRequestIgnoreOptions.SectionName));

        services.Configure<WebhookSecretsOptions>(
            configuration.GetSection(WebhookSecretsOptions.SectionName));

        services.Configure<FileExcludeOptions>(
            configuration.GetSection(FileExcludeOptions.SectionName));

        services.AddScoped<ICodeReviewService, CodeReviewService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        return services;
    }
}
