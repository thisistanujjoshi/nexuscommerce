using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;

namespace Orders.Application.Abstractions;

/// <summary>
/// EF Core-backed persistence abstraction. Unlike Catalog's repository layer,
/// the Orders service exposes DbSets directly to CQRS handlers — command handlers
/// load and mutate aggregates, query handlers project straight to DTOs.
/// (See ADR 0002 for the deliberate contrast between the two services.)
/// </summary>
public interface IOrdersDbContext
{
    DbSet<Order> Orders { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
