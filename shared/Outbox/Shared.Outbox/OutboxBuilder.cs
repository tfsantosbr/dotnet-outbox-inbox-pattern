using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;

using Polly;

using Shared.Outbox.Metrics;
using Shared.Outbox.Resilience;
using Shared.Outbox.Settings;

namespace Shared.Outbox;

public sealed class OutboxBuilder(IServiceCollection services, string? moduleName, bool isKeyed = false)
{
    internal readonly string? ModuleName = moduleName;
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

    public OutboxBuilder WithMetrics(Action<OutboxMetricsOptions>? configure = null)
    {
        var options = new OutboxMetricsOptions();
        configure?.Invoke(options);

        var globalTags = BuildGlobalTags(options.Tags);

        if (isKeyed)
            Services.AddKeyedSingleton<IOutboxMetrics>(ModuleName, (sp, _) =>
                new OutboxMetrics(sp.GetRequiredService<IMeterFactory>(), globalTags));
        else
            Services.AddSingleton<IOutboxMetrics>(sp =>
                new OutboxMetrics(sp.GetRequiredService<IMeterFactory>(), globalTags));

        return this;
    }

    private Dictionary<string, string> BuildGlobalTags(IReadOnlyDictionary<string, string>? additionalTags)
    {
        var tags = new Dictionary<string, string>();

        if (additionalTags is not null)
            foreach (var (key, value) in additionalTags)
                tags[key] = value;

        if (ModuleName is not null)
            tags[OutboxInstrumentation.ModuleTagKey] = ModuleName;
        
        return tags;
    }
}
