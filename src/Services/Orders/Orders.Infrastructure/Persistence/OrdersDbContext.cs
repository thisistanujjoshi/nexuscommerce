using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options), IOrdersDbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
