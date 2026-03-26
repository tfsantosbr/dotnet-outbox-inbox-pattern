using Microsoft.EntityFrameworkCore;
using Shared.Outbox.Abstractions;

namespace Shared.Outbox.Database;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
