namespace Shared.Messaging.Abstractions;

public sealed class ConsumerOptions
{
    public string Exchange { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Fanout;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
}
