using System.Diagnostics.Metrics;

using Shared.Inbox.Abstractions.Metrics;

using Microsoft.Extensions.DependencyInjection;

namespace Shared.Inbox.Abstractions.Extensions;

public sealed class InboxBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    public InboxBuilder WithMetrics(Action<InboxMetricsOptions>? configure = null)
    {
        var options = new InboxMetricsOptions();
        configure?.Invoke(options);

        Services.AddSingleton<IInboxMetrics>(sp =>
            new InboxMetrics(sp.GetRequiredService<IMeterFactory>()));

        return this;
    }
}