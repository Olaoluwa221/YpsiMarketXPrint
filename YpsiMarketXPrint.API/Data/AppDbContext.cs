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
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<ArtworkUploadToken> ArtworkUploadTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Compound primary key for ProductPicture
            modelBuilder.Entity<ProductPicture>().HasKey(pp => new { pp.ProductId, pp.PictureId });

            // Compound primary key for CartItem - now uses VariantId
            modelBuilder.Entity<CartItem>().HasKey(ci => new { ci.CartId, ci.VariantId });

            // Compound primary key for OrderItem - now uses VariantId
            modelBuilder.Entity<OrderItem>().HasKey(oi => new { oi.OrderId, oi.VariantId });

            // OrderItem.ArtworkId -> Picture (optional, restrict delete so order history is preserved)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Artwork)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ArtworkId)
                .OnDelete(DeleteBehavior.Restrict);

            // ArtworkUploadToken -> OrderItem (composite FK)
            modelBuilder.Entity<ArtworkUploadToken>()
                .HasOne(t => t.OrderItem)
                .WithMany()
                .HasForeignKey(t => new { t.OrderId, t.VariantId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ArtworkUploadToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            // A single Stripe PaymentIntent can only claim one order
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.PaymentIntentId)
                .IsUnique();

            // Primary key for ProductVariant
            modelBuilder.Entity<ProductVariant>().HasKey(v => v.VariantId);

            // One user has one cart
            modelBuilder.Entity<Cart>().HasIndex(c => c.UserId).IsUnique();

            // Enum-like enforcement for UserType
            modelBuilder.Entity<User>().Property(u => u.UserType).HasConversion<string>();

            // Enum-like enforcement for OrderStatus
            modelBuilder.Entity<Order>().Property(o => o.OrderStatus).HasConversion<string>();

            // Enum-like enforcement for DeliveryMethod
            modelBuilder.Entity<Order>().Property(o => o.DeliveryMethod).HasConversion<string>();

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
