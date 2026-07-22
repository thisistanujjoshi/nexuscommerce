using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return context.Products.FirstOrDefaultAsync(p => p.Sku == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string? searchTerm, Guid? categoryId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(searchTerm, categoryId)
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchTerm, Guid? categoryId, CancellationToken cancellationToken = default) =>
        BuildQuery(searchTerm, categoryId).CountAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await context.Products.AddAsync(product, cancellationToken);

    public void Remove(Product product) => context.Products.Remove(product);

    private IQueryable<Product> BuildQuery(string? searchTerm, Guid? categoryId)
    {
        var query = context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = $"%{searchTerm.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, term) ||
                EF.Functions.Like(p.Description, term) ||
                EF.Functions.Like(p.Sku, term));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return query;
    }
}
