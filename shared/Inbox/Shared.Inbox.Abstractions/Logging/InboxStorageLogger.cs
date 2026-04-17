using Shared.Inbox.Abstractions.Models;

using Microsoft.Extensions.Logging;

namespace Shared.Inbox.Abstractions.Logging;

internal static class InboxStorageLogger
{
    public static void LogRegistered<T>(ILogger<T> logger, InboxMessage message)
    {
        logger.LogInformation(
            "Inbox message '{MessageId}' registered for consumer '{Consumer}'",
            message.MessageId,
            message.Consumer);
    }

    public static void LogDuplicate<T>(ILogger<T> logger, InboxMessage message)
    {
        logger.LogWarning(
            "Inbox message '{MessageId}' is duplicate for consumer '{Consumer}', skipping",
            message.MessageId,
            message.Consumer);
    }
}