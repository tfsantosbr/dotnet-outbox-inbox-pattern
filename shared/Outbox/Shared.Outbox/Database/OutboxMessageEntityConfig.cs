using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Shared.Outbox.Abstractions;

namespace Shared.Outbox.Database;

public class OutboxMessageEntityConfig(string tableName = "OutboxMessages") : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName).HasKey(p => p.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Type).HasMaxLength(500);
        builder.Property(o => o.Headers).HasColumnType("jsonb");
        builder.Property(o => o.Content).HasColumnType("jsonb");

        builder.HasIndex(o => o.OccurredOnUtc);
        builder.HasIndex(o => o.ProcessedOnUtc);
        builder.HasIndex(o => o.ErrorHandledOnUtc);

        builder.HasIndex(o => o.OccurredOnUtc)
            .HasFilter("\"ProcessedOnUtc\" IS NULL")
            .HasDatabaseName("IX_outbox_messages_pending");
    }
}