namespace Shared.Messaging.Abstractions;

public enum AckMode { Manual, AutoOnSuccess }

public class ConsumerOptions
{
    public AckMode AckMode { get; set; } = AckMode.Manual;

    public virtual void Validate() { }
}