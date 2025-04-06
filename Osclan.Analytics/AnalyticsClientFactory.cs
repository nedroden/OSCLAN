using System;
using Microsoft.Extensions.Logging;
using Osclan.Analytics.Abstractions;

namespace Osclan.Analytics;

public class AnalyticsClientFactory : IAnalyticsClientFactory, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    
    public AnalyticsClientFactory()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            }));
    }

    public AnalyticsClient<T> CreateClient<T>() =>
        new(_loggerFactory.CreateLogger<T>());

    public void Dispose() =>
        _loggerFactory.Dispose();
}