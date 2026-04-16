namespace Shared.Messaging.Abstractions.Models;

public enum AckMode { Manual, AutoOnSuccess }

public class ConsumerOptions
{
    public AckMode AckMode { get; set; } = AckMode.Manual;

    public virtual void Validate() { }
}