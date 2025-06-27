using Microsoft.EntityFrameworkCore;
using OrderService.Models.Entities;

namespace OrderService.Data
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
