using Microsoft.Extensions.DependencyInjection;

namespace InboxPattern.Abstractions.Extensions;

public static class InboxExtensions
{
    public static InboxBuilder AddInbox(this IServiceCollection services)
        => new(services);
}
