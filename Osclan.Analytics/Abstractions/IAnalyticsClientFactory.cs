namespace Osclan.Analytics.Abstractions;

public interface IAnalyticsClientFactory
{
    AnalyticsClient<T> CreateClient<T>();
}