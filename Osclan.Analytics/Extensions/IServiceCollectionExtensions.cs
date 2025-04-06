using Microsoft.Extensions.DependencyInjection;
using Osclan.Analytics.Abstractions;

namespace Osclan.Analytics.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOsclanAnalytics(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsClientFactory, AnalyticsClientFactory>();

        return services;
    }
}