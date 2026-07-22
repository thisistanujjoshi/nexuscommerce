using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Catalog persistence layer. The database provider is chosen via
    /// configuration ("Database:Provider": "SqlServer" | "Sqlite") so the service runs
    /// against SQL Server in real environments and SQLite for lightweight local dev.
    /// </summary>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";

        services.AddDbContext<CatalogDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    options.UseSqlite(
                        configuration.GetConnectionString("CatalogDb") ?? "Data Source=catalog.db");
                    break;
                case "sqlserver":
                    options.UseSqlServer(
                        configuration.GetConnectionString("CatalogDb")
                        ?? throw new InvalidOperationException(
                            "Connection string 'CatalogDb' is required when using the SqlServer provider."));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown database provider '{provider}'.");
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        return services;
    }
}
