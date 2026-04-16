using Shared.Messaging.Abstractions;

namespace Shared.Messaging.RabbitMQ.Options;

public sealed class RabbitMqConsumerOptions : ConsumerOptions
{
    public string Exchange { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public RabbitMqExchangeType ExchangeType { get; set; } = RabbitMqExchangeType.Fanout;
    public bool Durable { get; set; } = false;
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
}