using Microsoft.Extensions.DependencyInjection;

using Polly;

using Shared.Outbox.Resilience;
using Shared.Outbox.Settings;

namespace Shared.Outbox;

public sealed class OutboxBuilder(IServiceCollection services, string moduleName)
{
    internal readonly string ModuleName = moduleName;
    internal OutboxStorageOptions StorageOptions { get; private set; } = new();
    internal OutboxProcessorOptions ProcessorOptions { get; private set; } = new();
    internal ResiliencePipeline ResiliencePipeline { get; private set; } = OutboxResilience.CreateDefault();

    public IServiceCollection Services { get; } = services;

    public OutboxBuilder UsePostgresStorage(Action<OutboxStorageOptions> configure)
    {
        configure(StorageOptions);
        return this;
    }

    public OutboxBuilder WithSettings(Action<OutboxProcessorOptions> configure)
    {
        configure(ProcessorOptions);
        return this;
    }

    public OutboxBuilder WithResilience(ResiliencePipeline pipeline)
    {
        ResiliencePipeline = pipeline;
        return this;
    }
}
