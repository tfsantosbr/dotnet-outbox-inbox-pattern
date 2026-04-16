using Shared.Outbox.Settings;

namespace Shared.Outbox.Tests.Settings;

public class OutboxStorageOptionsValidationTests
{
    [Fact]
    public void Validate_WhenConnectionStringIsEmpty_ShouldThrowArgumentException()
    {
        var options = new OutboxStorageOptions();

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("ConnectionString is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenSchemaIsEmpty_ShouldThrowArgumentException()
    {
        var options = new OutboxStorageOptions
        {
            ConnectionString = "Host=localhost",
            Schema = ""
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("Schema is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenTableNameIsEmpty_ShouldThrowArgumentException()
    {
        var options = new OutboxStorageOptions
        {
            ConnectionString = "Host=localhost",
            TableName = ""
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("TableName is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenMultipleFieldsAreInvalid_ShouldIncludeAllErrors()
    {
        var options = new OutboxStorageOptions
        {
            ConnectionString = "",
            Schema = "",
            TableName = ""
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("ConnectionString is required", ex.Message);
        Assert.Contains("Schema is required", ex.Message);
        Assert.Contains("TableName is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenAllFieldsAreValid_ShouldNotThrow()
    {
        var options = new OutboxStorageOptions
        {
            ConnectionString = "Host=localhost",
            Schema = "public",
            TableName = "OutboxMessages"
        };

        options.Validate();
    }
}

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
