using Microsoft.EntityFrameworkCore;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;

namespace Shared.Outbox.Tests.Database;

public class OutboxMessageEntityConfigTests
{
    [Fact]
    public void Configure_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new OutboxMessageEntityConfig();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => config.Configure(null!));
    }

    [Fact]
    public void Configure_ShouldMapToOutboxMessagesTable()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new OutboxMessageEntityConfig();

        // Act
        modelBuilder.ApplyConfiguration(config);

        // Assert
        var entity = modelBuilder.Model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(entity);
        Assert.Equal("OutboxMessages", entity.GetTableName());
    }

    [Fact]
    public void Configure_ShouldSetIdAsPrimaryKey()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new OutboxMessageEntityConfig();

        // Act
        modelBuilder.ApplyConfiguration(config);

        // Assert
        var entity = modelBuilder.Model.FindEntityType(typeof(OutboxMessage));
        var primaryKey = entity!.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties, p => p.Name == nameof(OutboxMessage.Id));
    }

    [Fact]
    public void Configure_ShouldSetTypeMaxLength()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new OutboxMessageEntityConfig();

        // Act
        modelBuilder.ApplyConfiguration(config);

        // Assert
        var entity = modelBuilder.Model.FindEntityType(typeof(OutboxMessage));
        var typeProperty = entity!.FindProperty(nameof(OutboxMessage.Type));
        Assert.NotNull(typeProperty);
        Assert.Equal(500, typeProperty.GetMaxLength());
    }

    [Fact]
    public void Configure_ShouldSetJsonbColumnTypeForHeadersAndContent()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new OutboxMessageEntityConfig();

        // Act
        modelBuilder.ApplyConfiguration(config);

        // Assert
        var entity = modelBuilder.Model.FindEntityType(typeof(OutboxMessage));
        var headersProperty = entity!.FindProperty(nameof(OutboxMessage.Headers));
        var contentProperty = entity.FindProperty(nameof(OutboxMessage.Content));
        Assert.NotNull(headersProperty);
        Assert.NotNull(contentProperty);
        Assert.Equal("jsonb", headersProperty.GetColumnType());
        Assert.Equal("jsonb", contentProperty.GetColumnType());
    }
}
