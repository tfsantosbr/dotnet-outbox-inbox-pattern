using Microsoft.Extensions.DependencyInjection;

namespace Shared.Messaging.Abstractions.Extensions;

public static class MessagingExtensions
{
    public static MessagingBuilder AddMessaging(this IServiceCollection services)
        => new(services);
}
