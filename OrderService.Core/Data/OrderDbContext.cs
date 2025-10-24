using Microsoft.EntityFrameworkCore;
using OrderService.Core.Models.Entities;

namespace OrderService.Core.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.Property(e => e.CreatedBy).IsRequired();
                
                // Configure decimal properties with precision for SQL Server
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.TaxRate).HasPrecision(18, 4);
                entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                
                // Add indexes for commonly queried fields
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.CreatedAt);
                
                // Configure owned entity types for addresses
                entity.OwnsOne(e => e.ShippingAddress, sa =>
                {
                    sa.Property(a => a.AddressLine1).HasColumnName("ShippingAddressLine1").HasMaxLength(100);
                    sa.Property(a => a.AddressLine2).HasColumnName("ShippingAddressLine2").HasMaxLength(100);
                    sa.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(50);
                    sa.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(50);
                    sa.Property(a => a.ZipCode).HasColumnName("ShippingZipCode").HasMaxLength(20);
                    sa.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(2).HasDefaultValue("US");
                });
                
                entity.OwnsOne(e => e.BillingAddress, ba =>
                {
                    ba.Property(a => a.AddressLine1).HasColumnName("BillingAddressLine1").HasMaxLength(100);
                    ba.Property(a => a.AddressLine2).HasColumnName("BillingAddressLine2").HasMaxLength(100);
                    ba.Property(a => a.City).HasColumnName("BillingCity").HasMaxLength(50);
                    ba.Property(a => a.State).HasColumnName("BillingState").HasMaxLength(50);
                    ba.Property(a => a.ZipCode).HasColumnName("BillingZipCode").HasMaxLength(20);
                    ba.Property(a => a.Country).HasColumnName("BillingCountry").HasMaxLength(2).HasDefaultValue("US");
                });
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                
                // Configure decimal properties with precision for SQL Server
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.DiscountPercentage).HasPrecision(18, 4);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.ShippingCostPerItem).HasPrecision(18, 2);
                entity.Property(e => e.GiftWrapCost).HasPrecision(18, 2);
                entity.Property(e => e.RefundedAmount).HasPrecision(18, 2);
                
                // Add indexes for commonly queried fields
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
                
                // Configure relationship
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.Items)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
