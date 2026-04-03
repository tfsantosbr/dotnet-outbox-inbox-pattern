using Shared.Messaging.Abstractions;

namespace Shared.Messaging.RabbitMQ;

internal sealed class PublishTopologyRegistry(IEnumerable<PublishTopologyEntry> entries)
    : IPublishTopologyRegistry
{
    private readonly IReadOnlyDictionary<Type, PublishOptions> _map =
        entries.ToDictionary(e => e.MessageType, e => e.Options);

    public PublishOptions? GetOptions(Type messageType) =>
        _map.TryGetValue(messageType, out var opts) ? opts : null;
}