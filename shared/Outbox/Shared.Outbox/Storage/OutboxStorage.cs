using Dapper;
using Npgsql;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Settings;

namespace Shared.Outbox.Storage;

public class OutboxStorage(OutboxSettings settings) : IOutboxStorage
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public async Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync(
        CancellationToken cancellationToken
    )
    {
        _connection = new NpgsqlConnection(settings.ConnectionString);
        await _connection.OpenAsync(cancellationToken);

        _transaction = await _connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            SELECT * 
            FROM "OutboxMessages"
            WHERE "ProcessedOn" IS NULL 
            ORDER BY "OccurredOn"
            LIMIT @MessagesBatchSize
            FOR UPDATE;
            """;

        var messages = await _connection.QueryAsync<OutboxMessage>(
            sql,
            new { settings.MessagesBatchSize },
            _transaction
        );

        return messages.AsList();
    }

    public async Task UpdateMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "OutboxMessages"
            SET "ProcessedOn" = @ProcessedOn,
                "Error" = @Error,
                "ErrorHandledOn" = @ErrorHandledOn
            WHERE "Id" = @Id;
            """;

        await _connection!.ExecuteAsync(
            sql,
            new
            {
                message.ProcessedOn,
                message.Error,
                message.ErrorHandledOn,
                message.Id,
            },
            _transaction
        );
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _transaction!.CommitAsync(cancellationToken);

        await _transaction.DisposeAsync();
        await _connection!.DisposeAsync();
    }
}
