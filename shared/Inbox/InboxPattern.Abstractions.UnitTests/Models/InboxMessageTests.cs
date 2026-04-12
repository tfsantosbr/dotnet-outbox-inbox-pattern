using FluentAssertions;

using InboxPattern.Abstractions.Models;

namespace InboxPattern.Abstractions.UnitTests.Models;

public class InboxMessageTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsInboxMessage()
    {
        var processedOn = DateTime.UtcNow;
        var message = InboxMessage.Create("msg-1", "consumer-a", processedOn);

        message.MessageId.Should().Be("msg-1");
        message.Consumer.Should().Be("consumer-a");
        message.ProcessedOnUtc.Should().Be(processedOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceMessageId_ThrowsArgumentException(string? messageId)
    {
        var act = () => InboxMessage.Create(messageId!, "consumer-a", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceConsumer_ThrowsArgumentException(string? consumer)
    {
        var act = () => InboxMessage.Create("msg-1", consumer!, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_PreservesProcessedOnUtc()
    {
        var expected = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var message = InboxMessage.Create("msg-1", "consumer-a", expected);

        message.ProcessedOnUtc.Should().Be(expected);
    }
}
