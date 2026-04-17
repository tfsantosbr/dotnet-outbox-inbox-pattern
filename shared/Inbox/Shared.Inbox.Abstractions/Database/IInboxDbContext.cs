using Shared.Inbox.Abstractions.Models;

using Microsoft.EntityFrameworkCore;

namespace Shared.Inbox.Abstractions.Database;

public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
}