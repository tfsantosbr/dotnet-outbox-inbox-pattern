namespace Shared.Messaging.Abstractions;

public interface IPublishTopologyRegistry
{
    PublishOptions? GetOptions(Type messageType);
}
