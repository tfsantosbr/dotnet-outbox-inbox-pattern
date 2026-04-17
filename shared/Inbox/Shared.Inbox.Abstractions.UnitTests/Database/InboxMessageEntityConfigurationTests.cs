using FluentAssertions;

using Shared.Inbox.Abstractions.Database;
using Shared.Inbox.Abstractions.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Shared.Inbox.Abstractions.UnitTests.Database;

public class InboxMessageEntityConfigurationTests
{
    private static IEntityType BuildEntityType(string tableName = "inbox_messages")
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration(tableName));
        var model = modelBuilder.FinalizeModel();
        return model.FindEntityType(typeof(InboxMessage))!;
    }

    [Fact]
    public void DefaultTableName_IsInboxMessages()
    {
        var entityType = BuildEntityType();

        entityType.GetTableName().Should().Be("inbox_messages");
    }

    [Fact]
    public void CustomTableName_IsApplied()
    {
        var entityType = BuildEntityType("my_inbox");

        entityType.GetTableName().Should().Be("my_inbox");
    }

    [Fact]
    public void PrimaryKey_IsCompositeMessageIdAndConsumer()
    {
        var entityType = BuildEntityType();
        var pk = entityType.FindPrimaryKey()!;

        pk.Properties.Select(p => p.Name).Should().BeEquivalentTo(["MessageId", "Consumer"]);
    }

    [Fact]
    public void MessageId_HasMaxLength200()
    {
        var entityType = BuildEntityType();
        var property = entityType.FindProperty(nameof(InboxMessage.MessageId))!;

        property.GetMaxLength().Should().Be(200);
    }

    [Fact]
    public void Consumer_HasMaxLength200()
    {
        var entityType = BuildEntityType();
        var property = entityType.FindProperty(nameof(InboxMessage.Consumer))!;

        property.GetMaxLength().Should().Be(200);
    }
}