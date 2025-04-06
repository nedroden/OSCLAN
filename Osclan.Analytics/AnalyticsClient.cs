using Microsoft.Extensions.Logging;
using Osclan.Analytics.Abstractions;

namespace Osclan.Analytics;

/// <summary>
/// Currently just a wrapper around the ILogger interface, but will be
/// expanded to include the necessary functionality for deeper insights
/// in the compilation process.
/// </summary>
/// <typeparam name="T"></typeparam>
public class AnalyticsClient<T>(ILogger<T> logger) : IAnalyticsClient
{
    #if DEBUG
    public void LogEvent(string message) =>
        logger.LogInformation(message);

    public void LogWarning(string message) =>
        logger.LogWarning(message);
    #else
    public void LogEvent(string message) {}

    public void LogWarning(string message) {}
    #endif

    public void LogError(string message) =>
        logger.LogInformation(message);
}