using FluentAssertions;

using InboxPattern.Abstractions.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace InboxPattern.Abstractions.UnitTests.Extensions;

public class InboxExtensionsTests
{
    [Fact]
    public void AddInbox_ReturnsInboxBuilder()
    {
        var services = new ServiceCollection();

        var builder = services.AddInbox();

        builder.Should().NotBeNull().And.BeOfType<InboxBuilder>();
    }

    [Fact]
    public void AddInbox_BuilderServices_IsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var builder = services.AddInbox();

        builder.Services.Should().BeSameAs(services);
    }
}
