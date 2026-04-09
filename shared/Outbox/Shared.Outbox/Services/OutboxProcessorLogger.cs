using Microsoft.Extensions.Logging;

using Shared.Outbox.Abstractions;

namespace Shared.Outbox.Services;

internal static class OutboxProcessorLogger
{
    public static void LogPublished<T>(ILogger<T> logger, string? moduleName, OutboxMessage message)
    {
        if (moduleName is null)
            logger.LogInformation(
                "Published message '{MessageType}' with id '{Id}'",
                message.GetTypeName(),
                message.Id);
        else
            logger.LogInformation(
                "Published message '{MessageType}' with id '{Id}' from '{Module}'",
                message.GetTypeName(),
                message.Id,
                moduleName);
    }

    public static void LogFailed<T>(ILogger<T> logger, string? moduleName, Exception ex, OutboxMessage message)
    {
        if (moduleName is null)
            logger.LogError(
                ex,
                "Failed to publish message '{MessageType}' with id '{Id}'",
                message.GetTypeName(),
                message.Id);
        else
            logger.LogError(
                ex,
                "Failed to publish message '{MessageType}' with id '{Id}' from '{Module}'",
                message.GetTypeName(),
                message.Id,
                moduleName);
    }
}
