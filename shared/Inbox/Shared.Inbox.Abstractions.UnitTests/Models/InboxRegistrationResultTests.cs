using FluentAssertions;

using Shared.Inbox.Abstractions.Models;

namespace Shared.Inbox.Abstractions.UnitTests.Models;

public class InboxRegistrationResultTests
{
    [Fact]
    public void Registered_SetsStatusToRegistered()
    {
        var result = InboxRegistrationResult.Registered();

        result.Status.Should().Be(InboxRegistrationStatus.Registered);
    }

    [Fact]
    public void Registered_IsRegisteredIsTrue()
    {
        var result = InboxRegistrationResult.Registered();

        result.IsRegistered.Should().BeTrue();
        result.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public void Duplicate_SetsStatusToDuplicate()
    {
        var result = InboxRegistrationResult.Duplicate();

        result.Status.Should().Be(InboxRegistrationStatus.Duplicate);
    }

    [Fact]
    public void Duplicate_IsDuplicateIsTrue()
    {
        var result = InboxRegistrationResult.Duplicate();

        result.IsDuplicate.Should().BeTrue();
        result.IsRegistered.Should().BeFalse();
    }
}