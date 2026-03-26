using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Outbox.Abstractions;

namespace Shared.Outbox.Database;

public class OutboxMessageEntityConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OutboxMessages").HasKey(p => p.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Type).HasMaxLength(500);
        builder.Property(o => o.Headers).HasColumnType("jsonb");
        builder.Property(o => o.Content).HasColumnType("jsonb");

        builder.HasIndex(o => o.OccurredOn);
        builder.HasIndex(o => o.ProcessedOn);
        builder.HasIndex(o => o.ErrorHandledOn);
    }
}
