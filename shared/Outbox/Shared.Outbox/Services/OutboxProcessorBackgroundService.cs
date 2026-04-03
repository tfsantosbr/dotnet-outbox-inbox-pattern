using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Polly;

using Shared.Messaging.Abstractions;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Settings;
using Shared.Outbox.Storage;

namespace Shared.Outbox.Services;

public class OutboxProcessorBackgroundService(
    string moduleName,
    IMessageBus messageBus,
    ILogger<OutboxProcessorBackgroundService> logger,
    IOutboxStorage outboxStorage,
    ResiliencePipeline resiliencePipeline,
    OutboxSettings settings
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await outboxStorage.GetMessagesAsync(stoppingToken);

            foreach (var message in messages)
            {
                var headers = message.GetHeaders();

                using (logger.BeginScope(headers ?? []))
                {
                    await ProcessMessage(message, headers, stoppingToken);

                    await outboxStorage.UpdateMessageAsync(message, stoppingToken);
                }
            }

            await outboxStorage.CommitAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalInSeconds), stoppingToken);
        }
    }

    private async Task ProcessMessage(
        OutboxMessage message,
        Dictionary<string, string>? headers,
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
        mergedHeaders[MessageHeaders.OccurredOnUtc] = message.OccurredOn.ToString("O");

        return mergedHeaders;
    }
}