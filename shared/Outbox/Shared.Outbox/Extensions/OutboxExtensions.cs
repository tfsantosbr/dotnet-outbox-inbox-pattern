using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Abstractions;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;
using Shared.Outbox.Publisher;
using Shared.Outbox.Resilience;
using Shared.Outbox.Services;
using Shared.Outbox.Settings;
using Shared.Outbox.Storage;

namespace Shared.Outbox.Extensions;

public static class OutboxExtensions
{
    public static IServiceCollection AddOutboxServices<TDbContext>(
        this IServiceCollection services,
        string moduleName,
        string connectionString,
        int intervalInSeconds,
        int messagesBatchSize
    )
        where TDbContext : DbContext, IOutboxDbContext
    {
        services.AddKeyedScoped<IOutboxPublisher, OutboxPublisher<TDbContext>>(moduleName);

        services.AddHostedService(sp =>
        {
            var settings = new OutboxSettings
            {
                ConnectionString = connectionString,
                IntervalInSeconds = intervalInSeconds,
                MessagesBatchSize = messagesBatchSize,
            };

            var resiliencePipeline = OutboxResilience.CreateDefault();
            var logger = sp.GetRequiredService<ILogger<OutboxProcessorBackgroundService>>();

            var scope = sp.CreateScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var outboxStorage = new OutboxStorage(settings);

            var outboxProcessor = new OutboxProcessorBackgroundService(
                moduleName,
                messageBus,
                logger,
                outboxStorage,
                resiliencePipeline,
                settings
            );

            return outboxProcessor;
        });

        return services;
    }
}
