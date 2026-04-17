using FluentAssertions;

using Shared.Inbox.Abstractions.Models;

namespace Shared.Inbox.Abstractions.UnitTests.Models;

public class InboxMessageTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsInboxMessage()
    {
        var message = InboxMessage.Create("msg-1", "consumer-a");

        message.MessageId.Should().Be("msg-1");
        message.Consumer.Should().Be("consumer-a");
        message.ProcessedOnUtc.Should().BeNull();
        message.ErrorHandledOnUtc.Should().BeNull();
        message.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceMessageId_ThrowsArgumentException(string? messageId)
    {
        var act = () => InboxMessage.Create(messageId!, "consumer-a");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceConsumer_ThrowsArgumentException(string? consumer)
    {
        var act = () => InboxMessage.Create("msg-1", consumer!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAsPublished_SetsProcessedOnUtcAndClearsError()
    {
        var message = InboxMessage.Create("msg-1", "consumer-a");
        var before = DateTime.UtcNow;

        message.MarkAsProcessed();

        var after = DateTime.UtcNow;
        message.ProcessedOnUtc.Should().NotBeNull();
        message.ProcessedOnUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        message.ErrorHandledOnUtc.Should().BeNull();
        message.Error.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessedWithError_SetsAllErrorFields()
    {
        var message = InboxMessage.Create("msg-1", "consumer-a");
        var before = DateTime.UtcNow;

        message.MarkAsProcessedWithError("something failed");

        var after = DateTime.UtcNow;
        message.ProcessedOnUtc.Should().NotBeNull();
        message.ProcessedOnUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        message.ErrorHandledOnUtc.Should().NotBeNull();
        message.ErrorHandledOnUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        message.Error.Should().Be("something failed");
    }

    [Fact]
    public void MarkAsPublished_AfterError_ClearsErrorFields()
    {
        var message = InboxMessage.Create("msg-1", "consumer-a");
        message.MarkAsProcessedWithError("transient error");

        message.MarkAsProcessed();

        message.ProcessedOnUtc.Should().NotBeNull();
        message.ErrorHandledOnUtc.Should().BeNull();
        message.Error.Should().BeNull();
    }
}