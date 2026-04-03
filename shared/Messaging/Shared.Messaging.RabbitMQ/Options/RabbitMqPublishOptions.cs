using Shared.Messaging.Abstractions;

namespace Shared.Messaging.RabbitMQ.Options;

public sealed class RabbitMqPublishOptions : PublishOptions
{
    public string RoutingKey { get; set; } = string.Empty;
    public RabbitMqExchangeType ExchangeType { get; set; } = RabbitMqExchangeType.Fanout;
    public bool Durable { get; set; } = false;
}

public enum RabbitMqExchangeType
{
    Fanout,
    Direct,
    Topic,
    Headers
}
