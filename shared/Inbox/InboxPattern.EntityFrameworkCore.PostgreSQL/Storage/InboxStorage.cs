using InboxPattern.Abstractions.Database;
using InboxPattern.Abstractions.Interfaces;
using InboxPattern.Abstractions.Logging;
using InboxPattern.Abstractions.Metrics;
using InboxPattern.Abstractions.Models;
using InboxPattern.EntityFrameworkCore.PostgreSQL.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NpgsqlTypes;

using Npgsql;

namespace InboxPattern.EntityFrameworkCore.PostgreSQL.Storage;

internal sealed class InboxStorage<TContext>(
    TContext context,
    IOptions<InboxStorageOptions> storageOptions,
    ILogger<InboxStorage<TContext>> logger,
    IInboxMetrics? metrics = null)
    : IInboxStorage
    where TContext : DbContext, IInboxDbContext
{
    private readonly InboxStorageOptions _options = storageOptions.Value;

    public async Task<InboxRegistrationResult> TryRegisterAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            INSERT INTO "{_options.Schema}"."{_options.TableName}" (message_id, consumer, processed_on_utc)
            VALUES (@messageId, @consumer, @processedOnUtc)
            ON CONFLICT (message_id, consumer) DO NOTHING
            """;

        var affected = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("messageId", message.MessageId),
                new NpgsqlParameter("consumer", message.Consumer),
                new NpgsqlParameter("processedOnUtc", NpgsqlDbType.TimestampTz)
                {
                    Value = DateTime.SpecifyKind(message.ProcessedOnUtc, DateTimeKind.Utc)
                }
            ],
            cancellationToken);

        var result = affected == 1
            ? InboxRegistrationResult.Registered()
            : InboxRegistrationResult.Duplicate();

        if (result.IsRegistered)
        {
            InboxStorageLogger.LogRegistered(logger, message);
            metrics?.RecordRegistered();
        }
        else
        {
            InboxStorageLogger.LogDuplicate(logger, message);
            metrics?.RecordDuplicate();
        }

        return result;
    }
}
