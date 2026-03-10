using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NineYi.Ai.CodeReview.Application.Abstractions;
using NineYi.Ai.CodeReview.Application.Services;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Domain.Settings;
using NineYi.Ai.CodeReview.Infrastructure.Clients;
using NineYi.Ai.CodeReview.Infrastructure.Data;
using NineYi.Ai.CodeReview.Infrastructure.Http;
using NineYi.Ai.CodeReview.Infrastructure.Repositories;
using NineYi.Ai.CodeReview.Infrastructure.Services;

namespace NineYi.Ai.CodeReview.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<DifySettings>(configuration.GetSection(DifySettings.SectionName));

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
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();

        // HTTP Clients with retry policy
        var retryPolicy = HttpPolicies.GetRetryPolicy();

        services.AddHttpClient<IGitPlatformService, GitHubService>("GitHub")
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IGitPlatformService, GitLabService>("GitLab")
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IGitPlatformService, BitbucketService>("Bitbucket")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IDifyService, DifyService>("Dify")
            .AddPolicyHandler(retryPolicy);

        // Phase 2：新 IRepoHostClient 三個實作 + Factory
        // Bitbucket 的 named client 已在上方設定 AllowAutoRedirect = false，此處直接注入即可
        services.AddScoped<GitHubClient>();
        services.AddScoped<GitLabClient>();
        services.AddScoped<BitbucketClient>();
        services.AddScoped<IRepoHostClientFactory, RepoHostClientFactory>();

        // Dify Service
        services.AddScoped<IDifyService, DifyService>();

        return services;
    }
}
