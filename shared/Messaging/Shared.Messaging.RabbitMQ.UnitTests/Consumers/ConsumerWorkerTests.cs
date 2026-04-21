using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Consumers;
using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.UnitTests.Consumers;

public class RabbitMqConsumerWorkerTests
{
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _serviceProvider;
    private readonly FakeConsumer _consumer;

    public RabbitMqConsumerWorkerTests()
    {
        _channel         = Substitute.For<IChannel>();
        _scopeFactory    = Substitute.For<IServiceScopeFactory>();
        _scope           = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _consumer        = new FakeConsumer();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(FakeConsumer)).Returns(_consumer);
    }

    [Fact]
    public async Task ProcessMessageAsync_InvalidJson_NacksWithRequeueFalse()
    {
        var worker = BuildWorker();
        var ea     = BuildEventArgs("not valid json {{{}}}");

        await worker.ProcessMessageAsync(_channel, ea, CancellationToken.None);

        await _channel.Received(1).BasicNackAsync(
            ea.DeliveryTag,
            multiple: false,
            requeue: false,
            Arg.Any<CancellationToken>());
        await _channel.DidNotReceive().BasicAckAsync(
            Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_NullDeserialization_NacksWithRequeueFalse()
    {
        var worker = BuildWorker();
        var ea     = BuildEventArgs("null");

        await worker.ProcessMessageAsync(_channel, ea, CancellationToken.None);

        await _channel.Received(1).BasicNackAsync(
            ea.DeliveryTag,
            multiple: false,
            requeue: false,
            Arg.Any<CancellationToken>());
        await _channel.DidNotReceive().BasicAckAsync(
            Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ConsumerReturnsAck_ManualMode_CallsBasicAck()
    {
        _consumer.Handler = (_, _, _) => Task.FromResult(ConsumerResult.Ack());
        var worker = BuildWorker(ackMode: AckMode.Manual);
        var ea     = BuildEventArgs(ValidMessageJson());

        await worker.ProcessMessageAsync(_channel, ea, CancellationToken.None);

        await _channel.Received(1).BasicAckAsync(
            ea.DeliveryTag,
            multiple: false,
            Arg.Any<CancellationToken>());
        await _channel.DidNotReceive().BasicNackAsync(
            Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ConsumerReturnsNack_ManualMode_CallsBasicNack()
    {
        _consumer.Handler = (_, _, _) => Task.FromResult(ConsumerResult.Nack(requeue: false));
        var worker = BuildWorker(ackMode: AckMode.Manual);
        var ea     = BuildEventArgs(ValidMessageJson());

        await worker.ProcessMessageAsync(_channel, ea, CancellationToken.None);

        await _channel.Received(1).BasicNackAsync(
            ea.DeliveryTag,
            multiple: false,
            requeue: false,
            Arg.Any<CancellationToken>());
        await _channel.DidNotReceive().BasicAckAsync(
            Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ConsumerThrows_NacksWithRequeueTrue()
    {
        _consumer.Handler = (_, _, _) => throw new Exception("processing error");
        var worker = BuildWorker();
        var ea     = BuildEventArgs(ValidMessageJson());

        await worker.ProcessMessageAsync(_channel, ea, CancellationToken.None);

        await _channel.Received(1).BasicNackAsync(
            ea.DeliveryTag,
            multiple: false,
            requeue: true,
            Arg.Any<CancellationToken>());
        await _channel.DidNotReceive().BasicAckAsync(
            Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_AutoOnSuccess_ConsumerSucceeds_CallsBasicAck()
    {
        _consumer.Handler = (_, _, _) => Task.FromResult(ConsumerResult.Nack());
        var worker = BuildWorker(ackMode: AckMode.AutoOnSuccess);
        var ea     = BuildEventArgs(ValidMessageJson());

        await worker.ProcessMessageAsync(_channel, ea, CancellationToken.None);

        await _channel.Received(1).BasicAckAsync(
            ea.DeliveryTag,
            multiple: false,
            Arg.Any<CancellationToken>());
        await _channel.DidNotReceive().BasicNackAsync(
            Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    private RabbitMqConsumerWorker<TestMessage, FakeConsumer> BuildWorker(
        AckMode ackMode = AckMode.Manual)
    {
        var connectionFactory = Substitute.For<IRabbitMqConnectionFactory>();
        ILogger<RabbitMqConsumerWorker<TestMessage, FakeConsumer>> logger =
            NullLogger<RabbitMqConsumerWorker<TestMessage, FakeConsumer>>.Instance;
        var options = new RabbitMqConsumerOptions
        {
            Exchange     = "test-exchange",
            Queue        = "test-queue",
            ConsumerName = "test-consumer",
            AckMode      = ackMode
        };

        return new RabbitMqConsumerWorker<TestMessage, FakeConsumer>(
            connectionFactory, _scopeFactory, options, logger);
    }

    private static BasicDeliverEventArgs BuildEventArgs(string json, ulong deliveryTag = 1)
    {
        var body  = Encoding.UTF8.GetBytes(json);
        var props = new BasicProperties();
        return new BasicDeliverEventArgs("tag", deliveryTag, false, "exchange", "rk", props, body);
    }

    private static string ValidMessageJson() =>
        JsonSerializer.Serialize(new TestMessage("hello"));

    private sealed record TestMessage(string Value);

    private sealed class FakeConsumer : IMessageConsumer<TestMessage>
    {
        public Func<TestMessage, IMessageContext, CancellationToken, Task<ConsumerResult>>? Handler { get; set; }

        public Task<ConsumerResult> ConsumeAsync(
            TestMessage message,
            IMessageContext context,
            CancellationToken cancellationToken) =>
            Handler is not null
                ? Handler(message, context, cancellationToken)
                : Task.FromResult(ConsumerResult.Ack());
    }
}
