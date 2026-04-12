using InboxPattern.Abstractions.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InboxPattern.Abstractions.Database;

public sealed class InboxMessageEntityConfiguration(string tableName = "inbox_messages")
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName);
        builder.HasKey(m => new { m.MessageId, m.Consumer });
        builder.Property(m => m.MessageId).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Consumer).HasMaxLength(200).IsRequired();
        builder.Property(m => m.ProcessedOnUtc).IsRequired();
    }
}
