using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Orders.API.Application.Orders;

namespace Orders.API.Infrastructure.Mappings;

public class OrderMapping : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.CreatedOnUtc)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
    }
}