using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;
using NineYi.Ai.CodeReview.Infrastructure.Repositories;
using NineYi.Ai.CodeReview.Infrastructure.Services;
using NineYi.Ai.CodeReview.Infrastructure.Services.WebhookParsers;
using Polly;
using Polly.Extensions.Http;

namespace NineYi.Ai.CodeReview.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database - SQLite
        services.AddDbContext<CodeReviewDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlite(connectionString);
        });

        // Repositories
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IReviewLogRepository, ReviewLogRepository>();
        services.AddScoped<IHotKeywordRepository, HotKeywordRepository>();
        services.AddScoped<IRuleStatisticsRepository, RuleStatisticsRepository>();
        services.AddScoped<IDifyUsageLogRepository, DifyUsageLogRepository>();

        // HTTP Clients with retry policy
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        services.AddHttpClient<IGitPlatformService, GitHubService>("GitHub")
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IGitPlatformService, GitLabService>("GitLab")
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IGitPlatformService, BitbucketService>("Bitbucket")
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IDifyService, DifyService>("Dify")
            .AddPolicyHandler(retryPolicy);

        // Git Platform Services
        services.AddScoped<IGitPlatformService, GitHubService>();
        services.AddScoped<IGitPlatformService, GitLabService>();
        services.AddScoped<IGitPlatformService, BitbucketService>();
        services.AddScoped<IGitPlatformServiceFactory, GitPlatformServiceFactory>();

        // Dify Service
        services.AddScoped<IDifyService, DifyService>();

        // Webhook Parsers
        services.AddScoped<IWebhookParserService, GitHubWebhookParser>();
        services.AddScoped<IWebhookParserService, GitLabWebhookParser>();
        services.AddScoped<IWebhookParserService, BitbucketWebhookParser>();

        return services;
    }
}
