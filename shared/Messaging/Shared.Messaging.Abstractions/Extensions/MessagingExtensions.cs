using Microsoft.Extensions.DependencyInjection;

namespace Shared.Messaging.Abstractions.Extensions;

public static class MessagingExtensions
{
    public static MessagingBuilder AddMessaging(this IServiceCollection services)
        => new(services);

    public static MessagingBuilder AddPublishOptions<TMessage>(
        this MessagingBuilder builder,
        Action<PublishOptions> configure)
    {
        var options = new PublishOptions();
        configure(options);
        builder.Services.AddSingleton(new PublishTopologyEntry(typeof(TMessage), options));
        return builder;
    }
}
