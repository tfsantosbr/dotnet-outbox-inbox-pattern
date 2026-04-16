namespace Shared.Messaging.Abstractions;

public readonly record struct ConsumerResult
{
    public ConsumerResultStatus Status { get; init; }
    public bool Requeue { get; init; }

    public bool IsAck => Status == ConsumerResultStatus.Ack;
    public bool IsNack => Status == ConsumerResultStatus.Nack;

    public static ConsumerResult Ack() =>
        new() { Status = ConsumerResultStatus.Ack };

    public static ConsumerResult Nack(bool requeue = true) =>
        new() { Status = ConsumerResultStatus.Nack, Requeue = requeue };
}

public enum ConsumerResultStatus { Ack, Nack }
