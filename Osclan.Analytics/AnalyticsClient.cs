using Microsoft.Extensions.Logging;
using Osclan.Analytics.Abstractions;

namespace Osclan.Analytics;

/// <summary>
/// Currently just a wrapper around the ILogger interface, but will be
/// expanded to include the necessary functionality for deeper insights
/// in the compilation process.
/// </summary>
/// <typeparam name="T"></typeparam>
public class AnalyticsClient<T> : IAnalyticsClient
{
    private readonly ILogger<T> _logger;

    public AnalyticsClient(ILogger<T> logger) => _logger = logger;

    public void LogEvent(string message) =>
        _logger.LogInformation(message);

    public void LogError(string message) =>
        _logger.LogInformation(message);
}