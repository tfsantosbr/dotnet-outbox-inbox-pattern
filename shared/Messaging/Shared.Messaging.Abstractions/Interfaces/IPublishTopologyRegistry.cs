using Shared.Messaging.Abstractions.Models;

namespace Shared.Messaging.Abstractions.Interfaces;

public interface IPublishTopologyRegistry
{
    PublishOptions? GetOptions(Type messageType);
}