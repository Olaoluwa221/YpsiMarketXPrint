using Microsoft.EntityFrameworkCore;
using YpsiMarketXPrint.API.Models;

namespace YpsiMarketXPrint.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Picture> Pictures { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductPicture> ProductPictures { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Compound primary key for ProductPicture
            modelBuilder.Entity<ProductPicture>().HasKey(pp => new { pp.ProductId, pp.PictureId });

            // Compound primary key for CartItem - now uses VariantId
            modelBuilder.Entity<CartItem>().HasKey(ci => new { ci.CartId, ci.VariantId });

            // Compound primary key for OrderItem - now uses VariantId
            modelBuilder.Entity<OrderItem>().HasKey(oi => new { oi.OrderId, oi.VariantId });

            // Primary key for ProductVariant
            modelBuilder.Entity<ProductVariant>().HasKey(v => v.VariantId);

            // One user has one cart
            modelBuilder.Entity<Cart>().HasIndex(c => c.UserId).IsUnique();

            // Enum-like enforcement for UserType
            modelBuilder.Entity<User>().Property(u => u.UserType).HasConversion<string>();

            // Enum-like enforcement for OrderStatus
            modelBuilder.Entity<Order>().Property(o => o.OrderStatus).HasConversion<string>();

            modelBuilder
                .Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(10,2)");

            modelBuilder
                .Entity<ProductVariant>()
                .Property(v => v.Price)
                .HasColumnType("decimal(10,2)");
        }
    }
}
