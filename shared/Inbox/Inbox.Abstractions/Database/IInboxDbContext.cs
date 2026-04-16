using Inbox.Abstractions.Models;

using Microsoft.EntityFrameworkCore;

namespace Inbox.Abstractions.Database;

public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
}
