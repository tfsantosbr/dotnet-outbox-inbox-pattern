using Inbox.Abstractions.Interfaces;
using Inbox.Abstractions.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Shared.Messaging.Abstractions;

namespace Inbox.Abstractions.Consumers;

public sealed class InboxConsumerDecorator<TMessage>(
    IMessageConsumer<TMessage> innerConsumer,
    DbContext dbContext,
    IInboxStorage inboxStorage,
    string consumerName,
    ILogger<InboxConsumerDecorator<TMessage>> logger)
    : IMessageConsumer<TMessage>
{
    public async Task<ConsumerResult> ConsumeAsync(
        TMessage message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var messageId = context.MessageId;

        if (string.IsNullOrWhiteSpace(messageId))
        {
            logger.LogWarning("Message without MessageId. Discarding without requeue.");
            return ConsumerResult.Nack(requeue: false);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var inboxMessage = InboxMessage.Create(
            messageId: messageId,
            consumer: consumerName);

        var inboxResult = await inboxStorage.TryRegisterAsync(inboxMessage, cancellationToken);

        if (inboxResult.IsDuplicate)
        {
            await transaction.CommitAsync(cancellationToken);
            return ConsumerResult.Ack();
        }

        try
        {
            var result = await innerConsumer.ConsumeAsync(message, context, cancellationToken);

            inboxMessage.MarkAsPublished();
            await inboxStorage.UpdateAsync(inboxMessage, cancellationToken);

            if (result.IsAck || result is { IsNack: true, Requeue: false })
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            inboxMessage.MarkAsProcessedWithError(ex.Message);
            await inboxStorage.UpdateAsync(inboxMessage, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw;
        }
    }
}
