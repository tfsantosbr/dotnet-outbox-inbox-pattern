namespace Shared.Messaging.Abstractions.Models;

public readonly record struct ConsumerResult
{
    public ConsumerResultStatus Status { get; init; }
    public bool Requeue { get; init; }
    public string? Error { get; init; }

    public bool IsAck => Status == ConsumerResultStatus.Ack;
    public bool IsNack => Status == ConsumerResultStatus.Nack;

    public static ConsumerResult Ack() =>
        new() { Status = ConsumerResultStatus.Ack };

    public static ConsumerResult Nack(bool requeue = true, string? error = null) =>
        new() { Status = ConsumerResultStatus.Nack, Requeue = requeue, Error = error };
}

public enum ConsumerResultStatus { Ack, Nack }