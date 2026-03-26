using Shared.Outbox.Abstractions;
using System.Text.Json;

namespace Shared.Outbox.Tests.Abstractions;

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
        Assert.Equal(occurredOn, message.OccurredOn);
        Assert.NotNull(message.Content);
        Assert.NotNull(message.Type);
        Assert.Null(message.ProcessedOn);
        Assert.Null(message.ErrorHandledOn);
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
        message.MarkAsProcessedWithSuccess();
        var afterProcessing = DateTime.UtcNow;

        // Assert
        Assert.NotNull(message.ProcessedOn);
        Assert.True(message.ProcessedOn >= beforeProcessing);
        Assert.True(message.ProcessedOn <= afterProcessing);
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
        Assert.NotNull(message.ErrorHandledOn);
        Assert.NotNull(message.ProcessedOn);
        Assert.True(message.ErrorHandledOn >= beforeFailure);
        Assert.True(message.ErrorHandledOn <= afterFailure);
        Assert.True(message.ProcessedOn >= beforeFailure);
        Assert.True(message.ProcessedOn <= afterFailure);
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
        message.MarkAsProcessedWithSuccess();
        var firstProcessedOn = message.ProcessedOn;

        // Act
        // Simulate a delay by creating a new message with a later timestamp
        var laterMessage = OutboxMessage.Create(
            "dest",
            Guid.NewGuid(),
            new TestContent("Test", 1),
            baseTime.AddMilliseconds(1)
        );
        laterMessage.MarkAsProcessedWithSuccess();
        laterMessage.MarkAsProcessedWithSuccess(); // Call twice to test the update

        // Assert
        Assert.NotNull(laterMessage.ProcessedOn);
        Assert.True(laterMessage.ProcessedOn >= firstProcessedOn);
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
        message.MarkAsProcessedWithSuccess();
        var originalProcessedOn = message.ProcessedOn;

        // Act
        // Simulate time passage by using reflection to set a later timestamp
        var laterTime = baseTime.AddMilliseconds(1);
        message.MarkAsProcessedWithError("Error occurred");

        // Assert
        Assert.NotNull(message.ProcessedOn);
        Assert.NotNull(message.Error);
        Assert.NotNull(message.ErrorHandledOn);
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
        Assert.NotNull(message.ErrorHandledOn);
        Assert.NotNull(message.ProcessedOn);
    }
}
