using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Outbox.Abstractions.Models;

namespace Outbox.Abstractions.Database;

public class OutboxMessageEntityConfig(string tableName = "OutboxMessages") : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName).HasKey(p => p.Id);

        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(o => o.Type).HasColumnName("type").HasMaxLength(500);
        builder.Property(o => o.Headers).HasColumnName("headers").HasColumnType("jsonb");
        builder.Property(o => o.Content).HasColumnName("content").HasColumnType("jsonb");
        builder.Property(o => o.Destination).HasColumnName("destination");
        builder.Property(o => o.OccurredOnUtc).HasColumnName("occurred_on_utc");
        builder.Property(o => o.PublishedOnUtc).HasColumnName("published_on_utc");
        builder.Property(o => o.ErrorHandledOnUtc).HasColumnName("error_handled_on_utc");
        builder.Property(o => o.Error).HasColumnName("error");

        builder.HasIndex(o => o.OccurredOnUtc);
        builder.HasIndex(o => o.PublishedOnUtc);
        builder.HasIndex(o => o.ErrorHandledOnUtc);

        builder.HasIndex(o => o.OccurredOnUtc)
            .HasFilter("\"published_on_utc\" IS NULL")
            .HasDatabaseName("IX_outbox_messages_pending");
    }
}
