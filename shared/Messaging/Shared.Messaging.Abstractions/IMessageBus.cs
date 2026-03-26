namespace Shared.Messaging.Abstractions;

public interface IMessageBus
{
    Task Publish(
        string message,
        string destination,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default
    );
}
