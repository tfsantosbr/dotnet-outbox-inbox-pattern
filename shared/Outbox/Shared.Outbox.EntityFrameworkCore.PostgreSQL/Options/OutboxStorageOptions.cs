namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.Options;

public record OutboxStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Schema { get; set; } = "public";
    public string TableName { get; set; } = "OutboxMessages";

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required.");

        if (string.IsNullOrWhiteSpace(Schema))
            errors.Add("Schema is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            errors.Add("TableName is required.");

        if (errors.Count > 0)
            throw new ArgumentException(
                $"Invalid {nameof(OutboxStorageOptions)}: {string.Join(" ", errors)}");
    }
}