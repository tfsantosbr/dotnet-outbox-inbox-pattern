using Shared.Outbox.EntityFrameworkCore.PostgreSQL.Options;

namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.UnitTests.Options;

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