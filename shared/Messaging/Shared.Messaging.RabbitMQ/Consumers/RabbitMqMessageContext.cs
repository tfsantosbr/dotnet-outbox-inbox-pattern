using System.Text;

using RabbitMQ.Client;

using Shared.Messaging.Abstractions.Interfaces;

namespace Shared.Messaging.RabbitMQ.Consumers;

internal sealed class RabbitMqMessageContext(
    IChannel channel,
    ulong deliveryTag,
    IDictionary<string, object?>? rawHeaders,
    string? messageId,
    bool redelivered) : IMessageContext
{
    public IReadOnlyDictionary<string, string> Headers { get; } = DecodeHeaders(rawHeaders);
    public string? MessageId { get; } = messageId;
    public bool Redelivered { get; } = redelivered;

    public async Task AckAsync(bool multiple = false, CancellationToken cancellationToken = default)
        => await channel.BasicAckAsync(deliveryTag, multiple: multiple, cancellationToken);

    public async Task NackAsync(bool multiple = false, bool requeue = true, CancellationToken cancellationToken = default)
        => await channel.BasicNackAsync(deliveryTag, multiple: multiple, requeue: requeue, cancellationToken);

    private static IReadOnlyDictionary<string, string> DecodeHeaders(IDictionary<string, object?>? rawHeaders)
    {
        if (rawHeaders is null)
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(rawHeaders.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in rawHeaders)
        {
            var decoded = value is byte[] bytes
                ? Encoding.UTF8.GetString(bytes)
                : value?.ToString() ?? string.Empty;
            result[key] = decoded;
        }
        return result;
    }
}