using Microsoft.Extensions.DependencyInjection;
using NineYi.Ai.CodeReview.Application.Services;

namespace NineYi.Ai.CodeReview.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICodeReviewService, CodeReviewService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        return services;
    }
}
