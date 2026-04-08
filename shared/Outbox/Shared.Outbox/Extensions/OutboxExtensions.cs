using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;
using Shared.Outbox.Metrics;
using Shared.Outbox.Publisher;
using Shared.Outbox.Services;
using Shared.Outbox.Storage;

namespace Shared.Outbox.Extensions;

public static class OutboxExtensions
{
    public static OutboxBuilder AddOutbox<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext, IOutboxDbContext
    {
        var builder = new OutboxBuilder(services, null);

        services.AddScoped<IOutboxPublisher, OutboxPublisher<TDbContext>>();

        services.AddTransient<IOutboxStorage>(
            _ => new OutboxStorage(
                Options.Create(builder.StorageOptions),
                Options.Create(builder.ProcessorOptions)));

        services.AddHostedService(sp =>
            new OutboxProcessor<TDbContext>(
                null,
                storageKey: null,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>(),
                builder.ResiliencePipeline,
                Options.Create(builder.ProcessorOptions),
                sp.GetService<IOutboxMetrics>()
            ));

        return builder;
    }

    public static OutboxBuilder AddKeyedOutbox<TDbContext>(
        this IServiceCollection services,
        string moduleName)
        where TDbContext : DbContext, IOutboxDbContext
    {
        var builder = new OutboxBuilder(services, moduleName, isKeyed: true);

        services.AddKeyedScoped<IOutboxPublisher, OutboxPublisher<TDbContext>>(moduleName);

        services.AddKeyedTransient<IOutboxStorage>(moduleName,
            (_, _) => new OutboxStorage(
                Options.Create(builder.StorageOptions),
                Options.Create(builder.ProcessorOptions)));

        services.AddHostedService(sp =>
            new OutboxProcessor<TDbContext>(
                moduleName,
                storageKey: moduleName,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>(),
                builder.ResiliencePipeline,
                Options.Create(builder.ProcessorOptions),
                sp.GetKeyedService<IOutboxMetrics>(moduleName)
            ));

        return builder;
    }
}
