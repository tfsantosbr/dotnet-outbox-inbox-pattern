using Polly;
using Polly.Retry;

namespace Shared.Outbox.Resilience;

public static class OutboxResilience
{
    public static ResiliencePipeline CreateDefault()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxRetryAttempts = 5,
                }
            )
            .Build();
    }
}
