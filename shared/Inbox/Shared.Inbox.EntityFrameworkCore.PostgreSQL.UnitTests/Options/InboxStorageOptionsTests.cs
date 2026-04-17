using FluentAssertions;

using Shared.Inbox.EntityFrameworkCore.PostgreSQL.Options;

namespace Shared.Inbox.EntityFrameworkCore.PostgreSQL.UnitTests.Options;

public class InboxStorageOptionsTests
{
    [Fact]
    public void DefaultSchema_IsPublic()
    {
        var options = new InboxStorageOptions();

        options.Schema.Should().Be("public");
    }

    [Fact]
    public void DefaultTableName_IsInboxMessages()
    {
        var options = new InboxStorageOptions();

        options.TableName.Should().Be("inbox_messages");
    }

    [Fact]
    public void Schema_CanBeChanged()
    {
        var options = new InboxStorageOptions { Schema = "inventory" };

        options.Schema.Should().Be("inventory");
    }

    [Fact]
    public void TableName_CanBeChanged()
    {
        var options = new InboxStorageOptions { TableName = "my_inbox" };

        options.TableName.Should().Be("my_inbox");
    }
}