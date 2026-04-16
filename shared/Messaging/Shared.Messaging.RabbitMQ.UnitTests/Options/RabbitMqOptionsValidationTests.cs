using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.UnitTests.Options;

public class RabbitMqOptionsValidationTests
{
    [Fact]
    public void Validate_WhenConnectionStringIsEmpty_ShouldThrowArgumentException()
    {
        var options = new RabbitMqOptions();

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("ConnectionString is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenConnectionStringIsProvided_ShouldNotThrow()
    {
        var options = new RabbitMqOptions { ConnectionString = "amqp://localhost" };

        options.Validate();
    }
}

public class RabbitMqConsumerOptionsValidationTests
{
    [Fact]
    public void Validate_WhenAllRequiredFieldsAreEmpty_ShouldThrowWithAllErrors()
    {
        var options = new RabbitMqConsumerOptions();

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("Exchange is required", ex.Message);
        Assert.Contains("Queue is required", ex.Message);
        Assert.Contains("ConsumerName is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenExchangeIsEmpty_ShouldThrowArgumentException()
    {
        var options = new RabbitMqConsumerOptions
        {
            Queue = "my-queue",
            ConsumerName = "my-consumer"
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("Exchange is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenQueueIsEmpty_ShouldThrowArgumentException()
    {
        var options = new RabbitMqConsumerOptions
        {
            Exchange = "my-exchange",
            ConsumerName = "my-consumer"
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("Queue is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenConsumerNameIsEmpty_ShouldThrowArgumentException()
    {
        var options = new RabbitMqConsumerOptions
        {
            Exchange = "my-exchange",
            Queue = "my-queue"
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("ConsumerName is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenDirectExchangeWithoutRoutingKey_ShouldThrowArgumentException()
    {
        var options = new RabbitMqConsumerOptions
        {
            Exchange = "my-exchange",
            Queue = "my-queue",
            ConsumerName = "my-consumer",
            ExchangeType = RabbitMqExchangeType.Direct
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("RoutingKey is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenTopicExchangeWithoutRoutingKey_ShouldThrowArgumentException()
    {
        var options = new RabbitMqConsumerOptions
        {
            Exchange = "my-exchange",
            Queue = "my-queue",
            ConsumerName = "my-consumer",
            ExchangeType = RabbitMqExchangeType.Topic
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("RoutingKey is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenFanoutExchangeWithoutRoutingKey_ShouldNotThrow()
    {
        var options = new RabbitMqConsumerOptions
        {
            Exchange = "my-exchange",
            Queue = "my-queue",
            ConsumerName = "my-consumer",
            ExchangeType = RabbitMqExchangeType.Fanout
        };

        options.Validate();
    }

    [Fact]
    public void Validate_WhenDirectExchangeWithRoutingKey_ShouldNotThrow()
    {
        var options = new RabbitMqConsumerOptions
        {
            Exchange = "my-exchange",
            Queue = "my-queue",
            ConsumerName = "my-consumer",
            ExchangeType = RabbitMqExchangeType.Direct,
            RoutingKey = "my-key"
        };

        options.Validate();
    }
}

public class RabbitMqPublishOptionsValidationTests
{
    [Fact]
    public void Validate_WhenDestinationIsEmpty_ShouldThrowArgumentException()
    {
        var options = new RabbitMqPublishOptions();

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("Destination is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenDirectExchangeWithoutRoutingKey_ShouldThrowArgumentException()
    {
        var options = new RabbitMqPublishOptions
        {
            Destination = "my-exchange",
            ExchangeType = RabbitMqExchangeType.Direct
        };

        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Contains("RoutingKey is required", ex.Message);
    }

    [Fact]
    public void Validate_WhenFanoutExchangeWithDestination_ShouldNotThrow()
    {
        var options = new RabbitMqPublishOptions
        {
            Destination = "my-exchange",
            ExchangeType = RabbitMqExchangeType.Fanout
        };

        options.Validate();
    }

    [Fact]
    public void Validate_WhenAllRequiredFieldsProvided_ShouldNotThrow()
    {
        var options = new RabbitMqPublishOptions
        {
            Destination = "my-exchange",
            ExchangeType = RabbitMqExchangeType.Direct,
            RoutingKey = "my-key"
        };

        options.Validate();
    }
}