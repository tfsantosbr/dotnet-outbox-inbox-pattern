namespace Shared.Messaging.RabbitMQ.Options;

public sealed class RabbitMqOptions
{
    /// <summary>
    /// The RabbitMQ connection string, e.g. "amqp://user:pass@host:port/vhost". Required.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Enables publisher confirms (AMQP confirm mode). When true, the broker acknowledges
    /// each published message, guaranteeing at-least-once delivery. Default is true.
    /// </summary>
    public bool PublisherConfirmationsEnabled { get; set; } = true;

    /// <summary>
    /// Enables per-message confirmation tracking so that individual publish failures
    /// can be correlated back to their originating call. Requires PublisherConfirmationsEnabled.
    /// Default is true.
    /// </summary>
    public bool PublisherConfirmationTrackingEnabled { get; set; } = true;

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required.");

        if (errors.Count > 0)
            throw new ArgumentException(
                $"Invalid {nameof(RabbitMqOptions)}: {string.Join(" ", errors)}");
    }
}