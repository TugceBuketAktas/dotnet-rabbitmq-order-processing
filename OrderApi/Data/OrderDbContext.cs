using Microsoft.EntityFrameworkCore;
using OrderModels.Models;

namespace OrderApi.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .Property(o => o.OrderId)
            .ValueGeneratedNever();

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.OrderItemId)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<OutboxMessage>()
            .Property(message => message.OutboxMessageId)
            .ValueGeneratedNever();
    }
}
