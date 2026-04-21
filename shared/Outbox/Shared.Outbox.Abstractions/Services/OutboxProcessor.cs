using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Outbox.Abstractions.Database;
using Shared.Outbox.Abstractions.Interfaces;
using Shared.Outbox.Abstractions.Metrics;
using Shared.Outbox.Abstractions.Models;
using Polly;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Shared.Outbox.Abstractions.Services;

internal sealed class OutboxProcessor<TContext>(
    string? moduleName,
    string? storageKey,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor<TContext>> logger,
    ResiliencePipeline resiliencePipeline,
    IOutboxMetrics? metrics = null)
    where TContext : DbContext, IOutboxDbContext
{
    internal async Task<bool> ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var outboxStorage = storageKey is null
            ? scope.ServiceProvider.GetRequiredService<IOutboxStorage>()
            : scope.ServiceProvider.GetRequiredKeyedService<IOutboxStorage>(storageKey);

        var cycleStopwatch = Stopwatch.StartNew();

        var messages = await outboxStorage.GetMessagesAsync(stoppingToken);

        if (messages.Count == 0) return false;

        metrics?.RecordBatchSize(messages.Count);

        var batchItems = messages
            .Select(m => new MessageBatchItem(m.Content, m.Destination, SetRequiredHeader(m, m.GetHeaders())))
            .ToList();

        var publishStopwatch = Stopwatch.StartNew();
        try
        {
            await resiliencePipeline.ExecuteAsync(async ct =>
                await messageBus.PublishBatchAsync(batchItems, ct), stoppingToken);

            publishStopwatch.Stop();
            metrics?.RecordPublishDuration(publishStopwatch.Elapsed.TotalMilliseconds);

            foreach (var m in messages)
            {
                m.MarkAsPublished();
                metrics?.RecordPublished();
                metrics?.RecordProcessed();
                OutboxProcessorLogger.LogPublished(logger, moduleName, m);
            }
        }
        catch (OperationCanceledException)
        {
            OutboxProcessorLogger.LogCancelled(logger, moduleName);
            throw;
        }
        catch (Exception ex)
        {
            publishStopwatch.Stop();
            metrics?.RecordPublishDuration(publishStopwatch.Elapsed.TotalMilliseconds);

            OutboxProcessorLogger.LogFailed(logger, moduleName, ex, messages[0]);
            foreach (var m in messages)
            {
                m.MarkAsProcessedWithError(ex.Message);
                metrics?.RecordFailed();
                metrics?.RecordProcessed();
            }
        }

        await outboxStorage.UpdateMessagesAsync([.. messages], stoppingToken);
        await outboxStorage.CommitAsync(stoppingToken);

        cycleStopwatch.Stop();
        metrics?.RecordCycleDuration(cycleStopwatch.Elapsed.TotalMilliseconds);

        return true;
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