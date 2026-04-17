using Shared.Inbox.Abstractions.Database;
using Shared.Inbox.Abstractions.Extensions;
using Shared.Inbox.Abstractions.Interfaces;
using Shared.Inbox.Abstractions.Metrics;
using Shared.Inbox.EntityFrameworkCore.PostgreSQL.Options;
using Shared.Inbox.EntityFrameworkCore.PostgreSQL.Storage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Shared.Inbox.EntityFrameworkCore.PostgreSQL.Extensions;

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