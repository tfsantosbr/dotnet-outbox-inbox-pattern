using Shared.Inbox.Abstractions.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shared.Inbox.Abstractions.Database;

public sealed class InboxMessageEntityConfiguration(string tableName = "inbox_messages")
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName);
        builder.HasKey(m => new { m.MessageId, m.Consumer });
        builder.Property(m => m.MessageId).HasColumnName("message_id").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Consumer).HasColumnName("consumer").HasMaxLength(200).IsRequired();
        builder.Property(m => m.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(m => m.ErrorHandledOnUtc).HasColumnName("error_handled_on_utc");
        builder.Property(m => m.Error).HasColumnName("error");
    }
}