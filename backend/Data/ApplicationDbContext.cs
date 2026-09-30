using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PendingSignup> PendingSignups => Set<PendingSignup>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVideo> ProductVideos => Set<ProductVideo>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ExternalLogin>().HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
        modelBuilder.Entity<PendingSignup>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<PendingSignup>().Property(x => x.Email).HasMaxLength(320).IsRequired();
        modelBuilder.Entity<PendingSignup>().Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
        modelBuilder.Entity<Product>().HasMany(x => x.Images).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Product>().HasMany(x => x.Videos).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductVariant>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<ProductVariant>().Property(x => x.Discount).HasPrecision(5, 2);
        modelBuilder.Entity<Product>().HasMany(x => x.Variants).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductVariant>().HasIndex(x => new { x.ProductId, x.Size }).IsUnique();
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().HasMany(x => x.Payments).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().HasIndex(x => x.RazorpayOrderId).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.RazorpayPaymentId).IsUnique().HasFilter("[RazorpayPaymentId] IS NOT NULL");
        modelBuilder.Entity<OrderItem>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Customer>().HasMany(x => x.ExternalLogins).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);
        modelBuilder.Entity<Customer>().HasMany(x => x.Orders).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);
        modelBuilder.Entity<CustomerAddress>().HasOne(x => x.Customer).WithMany(x => x.Addresses).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CustomerAddress>().Property(x => x.Name).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<CustomerAddress>().Property(x => x.Mobile).HasMaxLength(30).IsRequired();
        modelBuilder.Entity<CustomerAddress>().Property(x => x.AddressLine1).HasMaxLength(500).IsRequired();
        modelBuilder.Entity<CustomerAddress>().Property(x => x.AddressLine2).HasMaxLength(500);
        modelBuilder.Entity<CustomerAddress>().Property(x => x.City).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<CustomerAddress>().Property(x => x.State).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<CustomerAddress>().Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<CustomerAddress>().Property(x => x.Country).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<Customer>().Property(x => x.Gender).HasMaxLength(30);
        modelBuilder.Entity<Customer>().Property(x => x.PasswordResetTokenHash).HasMaxLength(64);
        modelBuilder.Entity<InventoryTransaction>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AdminUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<WishlistItem>().HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
        modelBuilder.Entity<WishlistItem>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WishlistItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Category>().Property(x => x.Name).HasMaxLength(250).IsRequired();
        modelBuilder.Entity<Category>().Property(x => x.ImageUrl).HasMaxLength(2048).IsRequired();
        modelBuilder.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Product>().HasOne(x => x.CategoryNavigation).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

