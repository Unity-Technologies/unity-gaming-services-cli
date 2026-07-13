using Microsoft.Extensions.Hosting;
using Unity.Analytics.Sender;

namespace Unity.Services.Cli.Common.Services;

class AnalyticsInitializer : IHostedService
{
    readonly IServiceProvider m_ServiceProvider;

    public AnalyticsInitializer(IServiceProvider serviceProvider)
    {
        m_ServiceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        m_ServiceProvider.InitAnalytics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
