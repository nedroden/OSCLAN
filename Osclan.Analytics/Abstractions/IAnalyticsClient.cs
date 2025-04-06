namespace Osclan.Analytics.Abstractions;

public interface IAnalyticsClient
{
    public void LogEvent(string message);

    public void LogError(string message);
}