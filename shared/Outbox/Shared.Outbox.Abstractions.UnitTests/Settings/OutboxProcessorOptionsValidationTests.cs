using Shared.Outbox.Abstractions.Settings;

namespace Shared.Outbox.Abstractions.UnitTests.Settings;

public class OutboxProcessorOptionsValidationTests
{
    [Fact]
    public void Validate_WhenIntervalIsZero_ShouldThrowArgumentException()
    {
        var options = new OutboxProcessorOptions { IntervalInSeconds = 0 };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("IntervalInSeconds must be greater than 0", ex.Message);
    }

    [Fact]
    public void Validate_WhenBatchSizeIsNegative_ShouldThrowArgumentException()
    {
        var options = new OutboxProcessorOptions { BatchSize = -1 };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("BatchSize must be greater than 0", ex.Message);
    }

    [Fact]
    public void Validate_WhenMaxParallelismIsZero_ShouldThrowArgumentException()
    {
        var options = new OutboxProcessorOptions { MaxParallelism = 0 };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("MaxParallelism must be greater than 0", ex.Message);
    }

    [Fact]
    public void Validate_WhenAllFieldsAreInvalid_ShouldIncludeAllErrors()
    {
        var options = new OutboxProcessorOptions
        {
            IntervalInSeconds = 0,
            BatchSize = 0,
            MaxParallelism = 0
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("IntervalInSeconds must be greater than 0", ex.Message);
        Assert.Contains("BatchSize must be greater than 0", ex.Message);
        Assert.Contains("MaxParallelism must be greater than 0", ex.Message);
    }

    [Fact]
    public void Validate_WhenDefaultValues_ShouldNotThrow()
    {
        var options = new OutboxProcessorOptions();

        options.Validate();
    }
}