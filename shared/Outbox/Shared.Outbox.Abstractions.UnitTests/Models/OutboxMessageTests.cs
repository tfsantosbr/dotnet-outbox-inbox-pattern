using System.Text.Json;

using Shared.Outbox.Abstractions.Models;

namespace Shared.Outbox.Abstractions.UnitTests.Models;

public class OutboxMessageTests
{
    private record TestContent(string Name, int Value);

    [Fact]
    public void Create_WhenCalledWithValidParameters_ShouldCreateOutboxMessage()
    {
        // Arrange
        var destination = "test-destination";
        var id = Guid.NewGuid();
        var content = new TestContent("Test", 123);
        var occurredOn = DateTime.UtcNow;

        // Act
        var message = OutboxMessage.Create(destination, id, content, occurredOn);

        // Assert
        Assert.Equal(id, message.Id);
        Assert.Equal(destination, message.Destination);
        Assert.Equal(occurredOn, message.OccurredOnUtc);
        Assert.NotNull(message.Content);
        Assert.NotNull(message.Type);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.ErrorHandledOnUtc);
        Assert.Null(message.Error);
    }

    [Fact]
    public void Create_WhenCalledWithNullHeaders_ShouldCreateMessageWithNullHeaders()
    {
        // Arrange
        var destination = "test-destination";
        var id = Guid.NewGuid();
        var content = new TestContent("Test", 123);
        var occurredOn = DateTime.UtcNow;

        // Act
        var message = OutboxMessage.Create(destination, id, content, occurredOn, null);

        // Assert
        Assert.Null(message.Headers);
    }

    [Fact]
    public void Create_WhenCalledWithHeaders_ShouldSerializeHeaders()
    {
        // Arrange
        var destination = "test-destination";
        var id = Guid.NewGuid();
        var content = new TestContent("Test", 123);
        var occurredOn = DateTime.UtcNow;
        var headers = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        // Act
        var message = OutboxMessage.Create(destination, id, content, occurredOn, headers);

        // Assert
        Assert.NotNull(message.Headers);
        Assert.NotEqual(string.Empty, message.Headers);

        var deserializedHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(
            message.Headers
        );
        Assert.Equal(headers, deserializedHeaders);
    }

    [Fact]
    public void Create_WhenCalledWithContent_ShouldSerializeContentAndSetType()
    {
        // Arrange
        var destination = "test-destination";
        var id = Guid.NewGuid();
        var content = new TestContent("Test", 123);
        var occurredOn = DateTime.UtcNow;

        // Act
        var message = OutboxMessage.Create(destination, id, content, occurredOn);

        // Assert
        Assert.Contains("TestContent", message.Type);

        var deserializedContent = JsonSerializer.Deserialize<TestContent>(message.Content);
        Assert.Equal(content.Name, deserializedContent?.Name);
        Assert.Equal(content.Value, deserializedContent?.Value);
    }

    [Fact]
    public void MarkAsProcessed_WhenCalled_ShouldSetProcessedOnToCurrentTime()
    {
        // Arrange
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            DateTime.UtcNow
        );
        var beforeProcessing = DateTime.UtcNow;

        // Act
        message.MarkAsPublished();
        var afterProcessing = DateTime.UtcNow;

        // Assert
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.True(message.ProcessedOnUtc >= beforeProcessing);
        Assert.True(message.ProcessedOnUtc <= afterProcessing);
    }

    [Fact]
    public void MarkAsFailed_WhenCalledWithError_ShouldSetErrorAndMarkAsProcessed()
    {
        // Arrange
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            DateTime.UtcNow
        );
        var errorMessage = "Test error message";
        var beforeFailure = DateTime.UtcNow;

        // Act
        message.MarkAsProcessedWithError(errorMessage);
        var afterFailure = DateTime.UtcNow;

        // Assert
        Assert.Equal(errorMessage, message.Error);
        Assert.NotNull(message.ErrorHandledOnUtc);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.True(message.ErrorHandledOnUtc >= beforeFailure);
        Assert.True(message.ErrorHandledOnUtc <= afterFailure);
        Assert.True(message.ProcessedOnUtc >= beforeFailure);
        Assert.True(message.ProcessedOnUtc <= afterFailure);
    }

    [Fact]
    public void GetHeaders_WhenHeadersIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            DateTime.UtcNow
        );

        // Act
        var headers = message.GetHeaders();

        // Assert
        Assert.Null(headers);
    }

    [Fact]
    public void GetHeaders_WhenHeadersExist_ShouldReturnDeserializedHeaders()
    {
        // Arrange
        var originalHeaders = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        };
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            DateTime.UtcNow,
            originalHeaders
        );

        // Act
        var headers = message.GetHeaders();

        // Assert
        Assert.NotNull(headers);
        Assert.Equal(2, headers.Count);
        Assert.True(headers.ContainsKey("key1"));
        Assert.True(headers.ContainsKey("key2"));
    }

    [Fact]
    public void Create_WhenCalledWithEmptyDictionary_ShouldSerializeEmptyDictionary()
    {
        // Arrange
        var destination = "test-destination";
        var id = Guid.NewGuid();
        var content = new TestContent("Test", 123);
        var occurredOn = DateTime.UtcNow;
        var emptyHeaders = new Dictionary<string, string>();

        // Act
        var message = OutboxMessage.Create(destination, id, content, occurredOn, emptyHeaders);

        // Assert
        Assert.NotNull(message.Headers);
        Assert.Equal("{}", message.Headers);
    }

    [Fact]
    public void MarkAsProcessed_WhenCalledMultipleTimes_ShouldUpdateProcessedOn()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            baseTime
        );
        message.MarkAsPublished();
        var firstProcessedOn = message.ProcessedOnUtc;

        // Act
        var laterMessage = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            baseTime.AddMilliseconds(1)
        );
        laterMessage.MarkAsPublished();
        laterMessage.MarkAsPublished();

        // Assert
        Assert.NotNull(laterMessage.ProcessedOnUtc);
        Assert.True(laterMessage.ProcessedOnUtc >= firstProcessedOn);
    }

    [Fact]
    public void MarkAsFailed_WhenCalledAfterMarkAsProcessed_ShouldOverrideProcessedOn()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            baseTime
        );
        message.MarkAsPublished();

        // Act
        message.MarkAsProcessedWithError("Error occurred");

        // Assert
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.NotNull(message.Error);
        Assert.NotNull(message.ErrorHandledOnUtc);
        Assert.Equal("Error occurred", message.Error);
    }

    [Fact]
    public void Create_WhenCalledWithNullContent_ShouldThrowException()
    {
        // Arrange
        var destination = "test-destination";
        var id = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<NullReferenceException>(
            () => OutboxMessage.Create<TestContent>(destination, id, null!, occurredOn)
        );
    }

    [Fact]
    public void MarkAsFailed_WhenCalledWithNullError_ShouldSetNullError()
    {
        // Arrange
        var message = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            DateTime.UtcNow
        );

        // Act
        message.MarkAsProcessedWithError(null!);

        // Assert
        Assert.Null(message.Error);
        Assert.NotNull(message.ErrorHandledOnUtc);
        Assert.NotNull(message.ProcessedOnUtc);
    }
}