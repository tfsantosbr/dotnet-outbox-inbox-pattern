using Shared.Inbox.Abstractions.Database;
using Shared.Inbox.Abstractions.Interfaces;
using Shared.Inbox.Abstractions.Logging;
using Shared.Inbox.Abstractions.Metrics;
using Shared.Inbox.Abstractions.Models;
using Shared.Inbox.EntityFrameworkCore.PostgreSQL.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Shared.Inbox.EntityFrameworkCore.PostgreSQL.Storage;

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
            INSERT INTO "{_options.Schema}"."{_options.TableName}" (message_id, consumer)
            VALUES (@messageId, @consumer)
            ON CONFLICT (message_id, consumer) DO NOTHING
            """;

        var affected = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("messageId", message.MessageId),
                new NpgsqlParameter("consumer", message.Consumer)
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

    public async Task UpdateAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            UPDATE "{_options.Schema}"."{_options.TableName}"
            SET processed_on_utc = @processedOnUtc,
                error_handled_on_utc = @errorHandledOnUtc,
                error = @error
            WHERE message_id = @messageId AND consumer = @consumer
            """;

        await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("processedOnUtc", NpgsqlDbType.TimestampTz)
                {
                    Value = message.ProcessedOnUtc.HasValue
                        ? DateTime.SpecifyKind(message.ProcessedOnUtc.Value, DateTimeKind.Utc)
                        : DBNull.Value
                },
                new NpgsqlParameter("errorHandledOnUtc", NpgsqlDbType.TimestampTz)
                {
                    Value = message.ErrorHandledOnUtc.HasValue
                        ? DateTime.SpecifyKind(message.ErrorHandledOnUtc.Value, DateTimeKind.Utc)
                        : DBNull.Value
                },
                new NpgsqlParameter("error", NpgsqlDbType.Text)
                {
                    Value = message.Error is not null ? message.Error : DBNull.Value
                },
                new NpgsqlParameter("messageId", message.MessageId),
                new NpgsqlParameter("consumer", message.Consumer)
            ],
            cancellationToken);
    }
}