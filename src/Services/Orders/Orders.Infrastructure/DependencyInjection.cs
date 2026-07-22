using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Orders persistence layer. The database provider is chosen via
    /// configuration ("Database:Provider": "PostgreSql" | "Sqlite") so the service runs
    /// against PostgreSQL in real environments and SQLite for lightweight local dev.
    /// </summary>
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "PostgreSql";

        services.AddDbContext<OrdersDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    options.UseSqlite(
                        configuration.GetConnectionString("OrdersDb") ?? "Data Source=orders.db");
                    break;
                case "postgresql":
                    options.UseNpgsql(
                        configuration.GetConnectionString("OrdersDb")
                        ?? throw new InvalidOperationException(
                            "Connection string 'OrdersDb' is required when using the PostgreSql provider."));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown database provider '{provider}'.");
            }
        });

        services.AddScoped<IOrdersDbContext>(sp => sp.GetRequiredService<OrdersDbContext>());

        return services;
    }
}
