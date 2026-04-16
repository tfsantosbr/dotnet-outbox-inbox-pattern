using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Outbox.Abstractions.Database;
using Outbox.Abstractions.Metrics;
using Outbox.Abstractions.Services;

namespace Outbox.Abstractions.Extensions;

public static class OutboxExtensions
{
    public static OutboxBuilder AddOutbox<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext, IOutboxDbContext
    {
        var builder = new OutboxBuilder(services, null);

        services.AddHostedService(sp =>
        {
            var processor = new OutboxProcessor<TDbContext>(
                null,
                storageKey: null,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>(),
                builder.ResiliencePipeline,
                sp.GetService<IOutboxMetrics>()
            );
            return new OutboxBackgroundService<TDbContext>(processor, Options.Create(builder.ProcessorOptions));
        });

        return builder;
    }

    public static OutboxBuilder AddKeyedOutbox<TDbContext>(
        this IServiceCollection services,
        string moduleName)
        where TDbContext : DbContext, IOutboxDbContext
    {
        var builder = new OutboxBuilder(services, moduleName, isKeyed: true);

        services.AddHostedService(sp =>
        {
            var processor = new OutboxProcessor<TDbContext>(
                moduleName,
                storageKey: moduleName,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>(),
                builder.ResiliencePipeline,
                sp.GetKeyedService<IOutboxMetrics>(moduleName)
            );
            return new OutboxBackgroundService<TDbContext>(processor, Options.Create(builder.ProcessorOptions));
        });

        return builder;
    }
}
