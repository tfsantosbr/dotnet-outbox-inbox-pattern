using Microsoft.Extensions.Options;

using Shared.Outbox.Abstractions;
using Shared.Outbox.Settings;
using Shared.Outbox.Storage;

namespace Shared.Outbox.Tests.Storage;

public class OutboxStorageTests
{
    private readonly IOptions<OutboxStorageOptions> _storageOptions;
    private readonly IOptions<OutboxProcessorOptions> _processorOptions;
    private readonly OutboxStorage _storage;

    public OutboxStorageTests()
    {
        _storageOptions = Options.Create(new OutboxStorageOptions
        {
            ConnectionString = "Data Source=:memory:;",
        });

        _processorOptions = Options.Create(new OutboxProcessorOptions
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
