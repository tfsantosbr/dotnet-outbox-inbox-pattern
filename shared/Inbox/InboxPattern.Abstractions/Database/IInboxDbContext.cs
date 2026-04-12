using InboxPattern.Abstractions.Models;

using Microsoft.EntityFrameworkCore;

namespace InboxPattern.Abstractions.Database;

public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
}
