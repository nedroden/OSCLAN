namespace Osclan.Analytics.Abstractions;

public interface IAnalyticsClient
{
    void LogEvent(string message);

    void LogError(string message);

    void LogWarning(string message);
}