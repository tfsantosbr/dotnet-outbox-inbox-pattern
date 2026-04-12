using InboxPattern.Abstractions.Database;
using InboxPattern.Abstractions.Extensions;
using InboxPattern.Abstractions.Interfaces;
using InboxPattern.Abstractions.Metrics;
using InboxPattern.EntityFrameworkCore.PostgreSQL.Options;
using InboxPattern.EntityFrameworkCore.PostgreSQL.Storage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace InboxPattern.EntityFrameworkCore.PostgreSQL.Extensions;

public static class InboxPostgreSQLExtensions
{
    public static InboxBuilder UsePostgreSQLStorage<TContext>(
        this InboxBuilder builder,
        Action<InboxStorageOptions>? configure = null)
        where TContext : DbContext, IInboxDbContext
    {
        var options = new InboxStorageOptions();
        configure?.Invoke(options);

        builder.Services.AddScoped<IInboxStorage>(sp =>
            new InboxStorage<TContext>(
                sp.GetRequiredService<TContext>(),
                MsOptions.Create(options),
                sp.GetRequiredService<ILogger<InboxStorage<TContext>>>(),
                sp.GetService<IInboxMetrics>()));

        return builder;
    }
}
