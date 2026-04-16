using Microsoft.Extensions.DependencyInjection;

namespace Shared.Messaging.Abstractions.Extensions;

public sealed class MessagingBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}