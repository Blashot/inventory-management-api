using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.DiscountApplied)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.OwnsMany(o => o.OrderLines, lb =>
        {
            lb.ToTable("order_lines");

            lb.WithOwner().HasForeignKey(l => l.OrderId);

            lb.HasKey(l => l.Id);

            lb.Property(l => l.ProductName)
                .IsRequired()
                .HasMaxLength(50);

            lb.Property(l => l.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            lb.Property(l => l.Quantity)
                .IsRequired();

            lb.Property(l => l.LineTotal)
                .IsRequired()
                .HasPrecision(18, 2);
        });

        builder.Navigation(o => o.OrderLines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

