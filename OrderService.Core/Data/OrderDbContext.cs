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
                entity.ToTable("orders"); // PostgreSQL table name convention
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.Property(e => e.CreatedBy).IsRequired();
                
                // Configure decimal properties with precision for PostgreSQL
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                
                // PostgreSQL-specific: Add index for commonly queried fields
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.CreatedAt);
                
                // Configure owned entity types for addresses
                entity.OwnsOne(e => e.ShippingAddress, sa =>
                {
                    sa.Property(a => a.AddressLine1).HasColumnName("shipping_address_line1").HasMaxLength(100);
                    sa.Property(a => a.AddressLine2).HasColumnName("shipping_address_line2").HasMaxLength(100);
                    sa.Property(a => a.City).HasColumnName("shipping_city").HasMaxLength(50);
                    sa.Property(a => a.State).HasColumnName("shipping_state").HasMaxLength(50);
                    sa.Property(a => a.ZipCode).HasColumnName("shipping_zip_code").HasMaxLength(20);
                    sa.Property(a => a.Country).HasColumnName("shipping_country").HasMaxLength(2).HasDefaultValue("US");
                });
                
                entity.OwnsOne(e => e.BillingAddress, ba =>
                {
                    ba.Property(a => a.AddressLine1).HasColumnName("billing_address_line1").HasMaxLength(100);
                    ba.Property(a => a.AddressLine2).HasColumnName("billing_address_line2").HasMaxLength(100);
                    ba.Property(a => a.City).HasColumnName("billing_city").HasMaxLength(50);
                    ba.Property(a => a.State).HasColumnName("billing_state").HasMaxLength(50);
                    ba.Property(a => a.ZipCode).HasColumnName("billing_zip_code").HasMaxLength(20);
                    ba.Property(a => a.Country).HasColumnName("billing_country").HasMaxLength(2).HasDefaultValue("US");
                });
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items"); // PostgreSQL table name convention
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                
                // Configure decimal properties with precision for PostgreSQL
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                
                // PostgreSQL-specific: Add indexes for commonly queried fields
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
