using Microsoft.Extensions.DependencyInjection;

namespace Shared.Messaging.Abstractions;

public sealed class MessagingBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}
