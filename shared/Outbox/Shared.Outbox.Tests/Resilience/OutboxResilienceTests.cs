using Polly;
using Shared.Outbox.Resilience;

namespace Shared.Outbox.Tests.Resilience;

public class OutboxResilienceTests
{
    [Fact]
    public void CreateDefault_ShouldReturnNonNullPipeline()
    {
        // Arrange & Act
        var pipeline = OutboxResilience.CreateDefault();

        // Assert
        Assert.NotNull(pipeline);
    }

    [Fact]
    public async Task CreateDefault_ShouldCreatePipelineThatExecutesSuccessfully()
    {
        // Arrange
        var pipeline = OutboxResilience.CreateDefault();
        var executed = false;

        // Act
        await pipeline.ExecuteAsync(_ =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        // Assert
        Assert.True(executed);
    }
}
