using System.Diagnostics;

using Dapper;

using Microsoft.Extensions.Options;

using Npgsql;

using Shared.Outbox.Abstractions;
using Shared.Outbox.Metrics;
using Shared.Outbox.Settings;

namespace Shared.Outbox.Storage;

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
            SELECT *
            FROM "{_storage.Schema}"."{_storage.TableName}"
            WHERE "ProcessedOnUtc" IS NULL
            ORDER BY "OccurredOnUtc"
            LIMIT @BatchSize
            FOR UPDATE;
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

    public async Task UpdateMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE "{_storage.Schema}"."{_storage.TableName}"
            SET "ProcessedOnUtc" = @ProcessedOnUtc,
                "Error" = @Error,
                "ErrorHandledOnUtc" = @ErrorHandledOnUtc
            WHERE "Id" = @Id;
            """;

        var stopwatch = Stopwatch.StartNew();

        await _connection!.ExecuteAsync(
            sql,
            new
            {
                message.ProcessedOnUtc,
                message.Error,
                message.ErrorHandledOnUtc,
                message.Id,
            },
            _transaction
        );

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
