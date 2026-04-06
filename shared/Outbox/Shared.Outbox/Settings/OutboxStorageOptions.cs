namespace Shared.Outbox.Settings;

public record OutboxStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Schema { get; set; } = "public";
    public string TableName { get; set; } = "OutboxMessages";
}
