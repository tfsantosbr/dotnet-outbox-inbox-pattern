using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;

using Shared.Messaging.Abstractions;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;
using Shared.Outbox.Metrics;
using Shared.Outbox.Settings;

namespace Shared.Outbox.Services;

internal sealed class OutboxProcessorBackgroundService<TContext>(
    string moduleName,
    string? storageKey,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorBackgroundService<TContext>> logger,
    ResiliencePipeline resiliencePipeline,
    IOptions<OutboxProcessorOptions> processorOptions,
    IOutboxMetrics? metrics = null
) : BackgroundService
    where TContext : DbContext, IOutboxDbContext
{
    private readonly OutboxProcessorOptions _processor = processorOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var outboxStorage = storageKey is null
                ? scope.ServiceProvider.GetRequiredService<IOutboxStorage>()
                : scope.ServiceProvider.GetRequiredKeyedService<IOutboxStorage>(storageKey);

            var messages = await outboxStorage.GetMessagesAsync(stoppingToken);

            foreach (var message in messages)
            {
                var headers = message.GetHeaders();

                using (logger.BeginScope(headers ?? []))
                {
                    await ProcessMessage(message, headers, messageBus, stoppingToken);

                    await outboxStorage.UpdateMessageAsync(message, stoppingToken);
                }
            }

            await outboxStorage.CommitAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(_processor.IntervalInSeconds), stoppingToken);
        }
    }

    private async Task ProcessMessage(
        OutboxMessage message,
        Dictionary<string, string>? headers,
        IMessageBus messageBus,
        CancellationToken stoppingToken
    )
    {
        try
        {
            await resiliencePipeline.ExecuteAsync(
                async cancelationToken =>
                {
                    var mergedHeaders = SetRequiredHeader(message, headers);

                    await messageBus.PublishAsync(
                        message.Content,
                        message.Destination,
                        mergedHeaders,
                        cancelationToken
                    );

                    message.MarkAsProcessedWithSuccess();

                    metrics?.RecordPublished();
                    metrics?.RecordProcessed();

                    logger.LogInformation(
                        "Published message '{MessageType}' with id '{Id}' from '{Module}'",
                        message.GetTypeName(),
                        message.Id,
                        moduleName
                    );
                },
                stoppingToken
            );
        }
        catch (Exception ex)
        {
            message.MarkAsProcessedWithError(ex.Message);

            metrics?.RecordFailed();
            metrics?.RecordProcessed();

            logger.LogError(
                ex,
                "Failed to publish message '{MessageType}' with id '{Id}' from '{Module}'",
                message.GetTypeName(),
                message.Id,
                moduleName
            );
        }
    }

    private static Dictionary<string, string> SetRequiredHeader(
        OutboxMessage message, Dictionary<string, string>? headers)
    {
        var mergedHeaders = headers is not null
                            ? new Dictionary<string, string>(headers)
                            : [];

        mergedHeaders[MessageHeaders.MessageId] = message.Id.ToString();
        mergedHeaders[MessageHeaders.OccurredOnUtc] = message.OccurredOnUtc.ToString("O");

        return mergedHeaders;
    }
}
