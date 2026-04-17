namespace Shared.Inbox.EntityFrameworkCore.PostgreSQL.Options;

public class InboxStorageOptions
{
    public string Schema { get; set; } = "public";
    public string TableName { get; set; } = "inbox_messages";
}