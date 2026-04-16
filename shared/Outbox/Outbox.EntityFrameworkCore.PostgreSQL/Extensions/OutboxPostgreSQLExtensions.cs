using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MsOptions = Microsoft.Extensions.Options.Options;

using Outbox.Abstractions.Database;
using Outbox.Abstractions.Extensions;
using Outbox.Abstractions.Interfaces;
using Outbox.Abstractions.Metrics;
using Outbox.EntityFrameworkCore.PostgreSQL.Options;
using Outbox.EntityFrameworkCore.PostgreSQL.Publisher;
using Outbox.EntityFrameworkCore.PostgreSQL.Storage;

namespace Outbox.EntityFrameworkCore.PostgreSQL.Extensions;

public static class OutboxPostgreSQLExtensions
{
    public static OutboxBuilder UsePostgreSQLStorage<TDbContext>(
        this OutboxBuilder builder,
        Action<OutboxStorageOptions> configure)
        where TDbContext : DbContext, IOutboxDbContext
    {
        var storageOptions = new OutboxStorageOptions();
        configure(storageOptions);
        storageOptions.Validate();

        if (builder.IsKeyed)
        {
            builder.Services.AddKeyedScoped<IOutboxPublisher, OutboxPublisher<TDbContext>>(builder.ModuleName);

            builder.Services.AddKeyedTransient<IOutboxStorage>(builder.ModuleName,
                (sp, _) => new OutboxStorage(
                    MsOptions.Create(storageOptions),
                    MsOptions.Create(builder.ProcessorOptions),
                    sp.GetKeyedService<IOutboxMetrics>(builder.ModuleName)));
        }
        else
        {
            builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher<TDbContext>>();

            builder.Services.AddTransient<IOutboxStorage>(sp =>
                new OutboxStorage(
                    MsOptions.Create(storageOptions),
                    MsOptions.Create(builder.ProcessorOptions),
                    sp.GetService<IOutboxMetrics>()));
        }

        return builder;
    }
}
