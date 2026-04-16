using Microsoft.EntityFrameworkCore;

using Outbox.Abstractions.Models;

namespace Outbox.Abstractions.Database;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
