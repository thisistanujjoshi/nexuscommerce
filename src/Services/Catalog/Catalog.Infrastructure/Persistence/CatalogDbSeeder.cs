using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public static class CatalogDbSeeder
{
    /// <summary>
    /// Creates the schema if needed and seeds demo data. Intended for local development;
    /// real environments use migrations via the deployment pipeline.
    /// </summary>
    public static async Task SeedAsync(CatalogDbContext context, CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);

        if (await context.Categories.AnyAsync(ct))
            return;

        var electronics = new Category("Electronics", "Devices, gadgets and accessories");
        var books = new Category("Books", "Physical and digital books");
        var homeGarden = new Category("Home & Garden", "Everything for the house and garden");

        context.Categories.AddRange(electronics, books, homeGarden);

        context.Products.AddRange(
            new Product("ELEC-KB-001", "Mechanical Keyboard", "87-key hot-swappable mechanical keyboard", 89.99m, 42, electronics.Id),
            new Product("ELEC-MS-002", "Wireless Mouse", "Ergonomic 2.4GHz wireless mouse", 24.50m, 120, electronics.Id),
            new Product("ELEC-MN-003", "27\" 4K Monitor", "IPS panel, 144Hz refresh rate", 379.00m, 15, electronics.Id),
            new Product("BOOK-CS-001", "Clean Architecture", "Robert C. Martin on software structure and design", 31.99m, 58, books.Id),
            new Product("BOOK-CS-002", "Designing Data-Intensive Applications", "Martin Kleppmann's guide to modern data systems", 44.99m, 33, books.Id),
            new Product("HOME-LT-001", "LED Desk Lamp", "Dimmable desk lamp with USB charging port", 34.95m, 77, homeGarden.Id));

        await context.SaveChangesAsync(ct);
    }
}
