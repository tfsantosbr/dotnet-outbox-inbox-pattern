namespace Shared.Outbox.Abstractions.Settings;

public record OutboxProcessorOptions
{
    public int IntervalInSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 30;
    public int MaxParallelism { get; set; } = 1;

    public void Validate()
    {
        var errors = new List<string>();

        if (IntervalInSeconds <= 0)
            errors.Add("IntervalInSeconds must be greater than 0.");

        if (BatchSize <= 0)
            errors.Add("BatchSize must be greater than 0.");

        if (MaxParallelism <= 0)
            errors.Add("MaxParallelism must be greater than 0.");

        if (errors.Count > 0)
            throw new ArgumentException(
                $"Invalid {nameof(OutboxProcessorOptions)}: {string.Join(" ", errors)}");
    }
}