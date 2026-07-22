using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);

        builder.Ignore(o => o.Total);

        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("OrderItems");
            item.WithOwner().HasForeignKey("OrderId");
            item.HasKey(i => i.Id);

            item.Property(i => i.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            item.Property(i => i.UnitPrice)
                .HasPrecision(18, 2);
        });

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
