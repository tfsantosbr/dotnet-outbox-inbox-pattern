using System.Diagnostics;

using Dapper;

using Microsoft.Extensions.Options;

using Npgsql;

using Shared.Outbox.Abstractions.Interfaces;
using Shared.Outbox.Abstractions.Metrics;
using Shared.Outbox.Abstractions.Models;
using Shared.Outbox.Abstractions.Settings;
using Shared.Outbox.EntityFrameworkCore.PostgreSQL.Options;

namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.Storage;

internal class OutboxStorage(
    IOptions<OutboxStorageOptions> storageOptions,
    IOptions<OutboxProcessorOptions> processorOptions,
    IOutboxMetrics? metrics = null)
    : IOutboxStorage, IAsyncDisposable
{
    private readonly OutboxStorageOptions _storage = storageOptions.Value;
    private readonly OutboxProcessorOptions _processor = processorOptions.Value;
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public async Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        _connection = new NpgsqlConnection(_storage.ConnectionString);
        await _connection.OpenAsync(cancellationToken);

        _transaction = await _connection.BeginTransactionAsync(cancellationToken);

        var sql = $"""
            SELECT
                "id"                   AS "Id",
                "headers"              AS "Headers",
                "type"                 AS "Type",
                "destination"          AS "Destination",
                "content"              AS "Content",
                "occurred_on_utc"      AS "OccurredOnUtc",
                "processed_on_utc"     AS "ProcessedOnUtc",
                "error_handled_on_utc" AS "ErrorHandledOnUtc",
                "error"                AS "Error"
            FROM "{_storage.Schema}"."{_storage.TableName}"
            WHERE "processed_on_utc" IS NULL
            ORDER BY "occurred_on_utc"
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED;
            """;

        var stopwatch = Stopwatch.StartNew();

        var messages = await _connection.QueryAsync<OutboxMessage>(
            sql,
            new { _processor.BatchSize },
            _transaction
        );

        stopwatch.Stop();
        metrics?.RecordFetchDuration(stopwatch.Elapsed.TotalMilliseconds);

        return messages.AsList();
    }

    public async Task UpdateMessagesAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0) return;

        var valuesList = string.Join(",",
            messages.Select((_, i) => $"(@Id{i}::uuid, @ProcessedOn{i}::timestamptz, @Error{i}::text, @ErrorHandledOn{i}::timestamptz)"));

        var sql = $"""
            UPDATE "{_storage.Schema}"."{_storage.TableName}"
            SET "processed_on_utc" = v.processed_on_utc,
                "error" = v.error,
                "error_handled_on_utc" = v.error_handled_on_utc
            FROM (VALUES
                {valuesList}
            ) AS v(id, processed_on_utc, error, error_handled_on_utc)
            WHERE "{_storage.Schema}"."{_storage.TableName}"."id" = v.id
            """;

        var parameters = new DynamicParameters();
        for (int i = 0; i < messages.Count; i++)
        {
            parameters.Add($"Id{i}", messages[i].Id.ToString());
            parameters.Add($"ProcessedOn{i}", messages[i].ProcessedOnUtc);
            parameters.Add($"Error{i}", messages[i].Error);
            parameters.Add($"ErrorHandledOn{i}", messages[i].ErrorHandledOnUtc);
        }

        var stopwatch = Stopwatch.StartNew();
        await _connection!.ExecuteAsync(sql, parameters, _transaction);
        stopwatch.Stop();
        metrics?.RecordUpdateDuration(stopwatch.Elapsed.TotalMilliseconds);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _transaction!.CommitAsync(cancellationToken);

        await _transaction.DisposeAsync();
        _transaction = null;

        await _connection!.DisposeAsync();
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}