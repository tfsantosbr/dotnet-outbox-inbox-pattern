using Microsoft.EntityFrameworkCore;

using Shared.Outbox.Abstractions.Models;

namespace Shared.Outbox.Abstractions.Database;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}