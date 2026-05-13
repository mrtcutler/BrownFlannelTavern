using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Data;

public class StoreDbContext : IdentityDbContext
{
    public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Lines)
            .WithOne(l => l.Order)
            .HasForeignKey(l => l.OrderId);

        // Seed some sample products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Classic Logo Tee", Description = "Soft cotton t-shirt with the Brown Flannel Tavern logo.", Price = 24.99m, Category = "T-Shirts", ImageUrl = "/images/classic-logo-tee.jpg" },
            new Product { Id = 2, Name = "Vintage Pub Hoodie", Description = "Warm hoodie with a vintage tavern design.", Price = 49.99m, Category = "Hoodies", ImageUrl = "/images/vintage-hoodie.jpg" },
            new Product { Id = 3, Name = "Tavern Baseball Cap", Description = "Adjustable cap with embroidered logo.", Price = 19.99m, Category = "Hats", ImageUrl = "/images/baseball-cap.jpg" }
        );

        modelBuilder.Entity<ProductVariant>().HasData(
            new ProductVariant { Id = 1, ProductId = 1, Size = "S", Color = "Black", StockQuantity = 25, WeightOz = 6m },
            new ProductVariant { Id = 2, ProductId = 1, Size = "M", Color = "Black", StockQuantity = 50, WeightOz = 6m },
            new ProductVariant { Id = 3, ProductId = 1, Size = "L", Color = "Black", StockQuantity = 40, WeightOz = 6m },
            new ProductVariant { Id = 4, ProductId = 1, Size = "XL", Color = "Black", StockQuantity = 30, WeightOz = 6m },
            new ProductVariant { Id = 5, ProductId = 1, Size = "S", Color = "Brown", StockQuantity = 20, WeightOz = 6m },
            new ProductVariant { Id = 6, ProductId = 1, Size = "M", Color = "Brown", StockQuantity = 35, WeightOz = 6m },
            new ProductVariant { Id = 7, ProductId = 1, Size = "L", Color = "Brown", StockQuantity = 30, WeightOz = 6m },
            new ProductVariant { Id = 8, ProductId = 1, Size = "XL", Color = "Brown", StockQuantity = 20, WeightOz = 6m },
            new ProductVariant { Id = 9, ProductId = 2, Size = "S", Color = "Charcoal", StockQuantity = 15, WeightOz = 18m },
            new ProductVariant { Id = 10, ProductId = 2, Size = "M", Color = "Charcoal", StockQuantity = 30, WeightOz = 18m },
            new ProductVariant { Id = 11, ProductId = 2, Size = "L", Color = "Charcoal", StockQuantity = 25, WeightOz = 18m },
            new ProductVariant { Id = 12, ProductId = 2, Size = "XL", Color = "Charcoal", StockQuantity = 20, WeightOz = 18m },
            new ProductVariant { Id = 13, ProductId = 3, Size = "One Size", Color = "Brown", StockQuantity = 50, WeightOz = 4m },
            new ProductVariant { Id = 14, ProductId = 3, Size = "One Size", Color = "Black", StockQuantity = 40, WeightOz = 4m }
        );
    }
}
