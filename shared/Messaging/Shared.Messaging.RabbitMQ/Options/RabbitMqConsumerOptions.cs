using Shared.Messaging.Abstractions.Models;

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
    public bool EnableDeadLetterQueue { get; set; }
    public string? DeadLetterExchange { get; set; }
    public string? DeadLetterRoutingKey { get; set; }
    public ushort PrefetchCount { get; set; } = 1;

    internal string ResolvedDeadLetterExchange => DeadLetterExchange ?? $"{Exchange}.dlq";
    internal string ResolvedDeadLetterQueue => $"{Queue}.dlq";
    internal string ResolvedDeadLetterRoutingKey => DeadLetterRoutingKey ?? $"{Queue}.dlq";

    public override void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Exchange))
            errors.Add("Exchange is required.");

        if (string.IsNullOrWhiteSpace(Queue))
            errors.Add("Queue is required.");

        if (string.IsNullOrWhiteSpace(ConsumerName))
            errors.Add("ConsumerName is required.");

        if (ExchangeType is RabbitMqExchangeType.Direct or RabbitMqExchangeType.Topic
            && string.IsNullOrWhiteSpace(RoutingKey))
            errors.Add("RoutingKey is required when ExchangeType is Direct or Topic.");

        if (errors.Count > 0)
            throw new ArgumentException(
                $"Invalid {nameof(RabbitMqConsumerOptions)}: {string.Join(" ", errors)}");
    }
}