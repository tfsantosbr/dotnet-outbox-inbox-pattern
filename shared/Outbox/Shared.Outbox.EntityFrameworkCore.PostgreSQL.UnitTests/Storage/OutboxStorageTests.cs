using Shared.Outbox.Abstractions.Models;
using Shared.Outbox.Abstractions.Settings;
using Shared.Outbox.EntityFrameworkCore.PostgreSQL.Options;
using Shared.Outbox.EntityFrameworkCore.PostgreSQL.Storage;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.UnitTests.Storage;

public class OutboxStorageTests
{
    private readonly Microsoft.Extensions.Options.IOptions<OutboxStorageOptions> _storageOptions;
    private readonly Microsoft.Extensions.Options.IOptions<OutboxProcessorOptions> _processorOptions;
    private readonly OutboxStorage _storage;

    public OutboxStorageTests()
    {
        _storageOptions = MsOptions.Create(new OutboxStorageOptions
        {
            ConnectionString = "Data Source=:memory:;",
        });

        _processorOptions = MsOptions.Create(new OutboxProcessorOptions
        {
            BatchSize = 10,
        });

        _storage = new OutboxStorage(_storageOptions, _processorOptions);
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldCreateInstance()
    {
        // Arrange & Act
        var storage = new OutboxStorage(_storageOptions, _processorOptions);

        // Assert
        Assert.NotNull(storage);
    }

    [Fact]
    public async Task GetMessagesAsync_WithInvalidConnectionString_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _storage.GetMessagesAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetMessagesAsync_WithCancelledToken_ShouldStillFailWithInvalidConnectionString()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _storage.GetMessagesAsync(cancellationTokenSource.Token)
        );
    }

    [Fact]
    public async Task UpdateMessagesAsync_WithoutOpenConnection_ShouldThrowException()
    {
        // Arrange
        var message = OutboxMessage.Create(
            "test-destination",
            Guid.NewGuid(),
            new { Name = "Test" },
            DateTime.UtcNow
        );

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            async () => await _storage.UpdateMessagesAsync([message], CancellationToken.None)
        );
    }

    [Fact]
    public async Task CommitAsync_WithoutOpenTransaction_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            async () => await _storage.CommitAsync(CancellationToken.None)
        );
    }
}