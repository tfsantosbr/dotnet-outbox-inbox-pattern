using System.Diagnostics;

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

internal sealed class OutboxProcessor<TContext>(
    string? moduleName,
    string? storageKey,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor<TContext>> logger,
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
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _processor.MaxParallelism,
                CancellationToken = stoppingToken
            };

            var processedAny = 0;

            await Parallel.ForEachAsync(
                Enumerable.Range(0, _processor.MaxParallelism),
                parallelOptions,
                async (_, token) =>
                {
                    var hadMessages = await ProcessMessages(token);
                    if (hadMessages) Interlocked.Increment(ref processedAny);
                });

            if (processedAny == 0)
                await Task.Delay(TimeSpan.FromSeconds(_processor.IntervalInSeconds), stoppingToken);
        }
    }

    private async Task<bool> ProcessMessages(CancellationToken stoppingToken)
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
            await resiliencePipeline.ExecuteAsync(
                async ct => await messageBus.PublishBatchAsync(batchItems, ct),
                stoppingToken);

            publishStopwatch.Stop();
            metrics?.RecordPublishDuration(publishStopwatch.Elapsed.TotalMilliseconds);

            foreach (var m in messages)
            {
                m.MarkAsProcessedWithSuccess();
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

            foreach (var m in messages)
            {
                m.MarkAsProcessedWithError(ex.Message);
                metrics?.RecordFailed();
                metrics?.RecordProcessed();
            }

            OutboxProcessorLogger.LogFailed(logger, moduleName, ex, messages[0]);
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
