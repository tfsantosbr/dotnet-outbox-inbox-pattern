using Shared.Messaging.Abstractions.Models;

namespace Shared.Messaging.RabbitMQ.Options;

public sealed class RabbitMqPublishOptions : PublishOptions
{
    public string RoutingKey { get; set; } = string.Empty;
    public RabbitMqExchangeType ExchangeType { get; set; } = RabbitMqExchangeType.Fanout;
    public bool Durable { get; set; } = false;

    public override void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Destination))
            errors.Add("Destination is required.");

        if (ExchangeType is RabbitMqExchangeType.Direct or RabbitMqExchangeType.Topic
            && string.IsNullOrWhiteSpace(RoutingKey))
            errors.Add("RoutingKey is required when ExchangeType is Direct or Topic.");

        if (errors.Count > 0)
            throw new ArgumentException(
                $"Invalid {nameof(RabbitMqPublishOptions)}: {string.Join(" ", errors)}");
    }
}

public enum RabbitMqExchangeType
{
    Fanout,
    Direct,
    Topic,
    Headers
}