using Microsoft.EntityFrameworkCore;
using SmartCartApi.Models;

namespace SmartCartApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasKey(p => p.Id);
        modelBuilder.Entity<Cart>().HasKey(c => c.Id);
        modelBuilder.Entity<CartItem>().HasKey(ci => ci.Id);
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId);
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId);

        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<OrderItem>().HasKey(oi => oi.Id);
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId);

        modelBuilder.Entity<Coupon>().HasKey(c => c.Id);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Wireless Headphones", Price = 299.99m, Stock = 50, Description = "Premium noise-cancelling headphones", Category = "Electronics" },
            new Product { Id = 2, Name = "Programming Book - C#", Price = 149.99m, Stock = 100, Description = "Complete guide to C# programming", Category = "Books" },
            new Product { Id = 3, Name = "Mechanical Keyboard", Price = 199.99m, Stock = 30, Description = "RGB mechanical gaming keyboard", Category = "Electronics" },
            new Product { Id = 4, Name = "USB-C Hub", Price = 79.99m, Stock = 75, Description = "7-in-1 USB-C hub adapter", Category = "Accessories" },
            new Product { Id = 5, Name = "Monitor Stand", Price = 129.99m, Stock = 40, Description = "Adjustable monitor stand with storage", Category = "Accessories" },
            new Product { Id = 6, Name = "Laptop Backpack", Price = 89.99m, Stock = 60, Description = "Water-resistant laptop backpack", Category = "Accessories" },
            new Product { Id = 7, Name = "Webcam HD", Price = 159.99m, Stock = 25, Description = "1080p HD webcam with microphone", Category = "Electronics" },
            new Product { Id = 8, Name = "Desk Lamp", Price = 59.99m, Stock = 80, Description = "LED desk lamp with adjustable brightness", Category = "Home Office" }
        );

        modelBuilder.Entity<Coupon>().HasData(
            new Coupon { Id = 1, Code = "FLAT50", Type = CouponType.Flat, DiscountValue = 50m, MinCartSubtotal = 500m, IsActive = true },
            new Coupon { Id = 2, Code = "SAVE10", Type = CouponType.Percentage, DiscountValue = 10m, MaxDiscount = 200m, MinCartSubtotal = 1000m, IsActive = true }
        );
    }
}
