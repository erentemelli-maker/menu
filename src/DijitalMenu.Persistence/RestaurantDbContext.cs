using DijitalMenu.Domain;
using Microsoft.EntityFrameworkCore;

namespace DijitalMenu.Persistence;

public sealed class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductExtra> ProductExtras => Set<ProductExtra>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentItem> PaymentItems => Set<PaymentItem>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<TableServiceRequest> TableServiceRequests => Set<TableServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>().Property(branch => branch.Name).HasMaxLength(120);
        modelBuilder.Entity<RestaurantTable>().HasIndex(table => new { table.BranchId, table.Number }).IsUnique();
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(product => product.ImageUrl).HasMaxLength(600);
        modelBuilder.Entity<ProductExtra>().Property(extra => extra.Price).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(item => item.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(payment => payment.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentItem>().Property(item => item.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Ignore(order => order.Total);
        modelBuilder.Entity<OrderItem>().Ignore(item => item.LineTotal);
        modelBuilder.Entity<StaffUser>().HasIndex(user => user.Username).IsUnique();
        modelBuilder.Entity<StaffUser>().Property(user => user.Username).HasMaxLength(64);
        modelBuilder.Entity<StaffUser>().Property(user => user.DisplayName).HasMaxLength(120);
        modelBuilder.Entity<StaffUser>().Property(user => user.PasswordHash).HasMaxLength(512);
        modelBuilder.Entity<Order>()
            .HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Payment>()
            .HasMany(payment => payment.Items)
            .WithOne()
            .HasForeignKey(item => item.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
