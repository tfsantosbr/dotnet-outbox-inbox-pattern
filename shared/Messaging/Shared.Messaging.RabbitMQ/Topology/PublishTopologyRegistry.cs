using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Shared.Messaging.RabbitMQ.Topology;

internal sealed class PublishTopologyRegistry(IEnumerable<PublishTopologyEntry> entries)
    : IPublishTopologyRegistry
{
    private readonly IReadOnlyDictionary<Type, PublishOptions> _map =
        entries.ToDictionary(e => e.MessageType, e => e.Options);

    public PublishOptions? GetOptions(Type messageType) =>
        _map.TryGetValue(messageType, out var opts) ? opts : null;
}