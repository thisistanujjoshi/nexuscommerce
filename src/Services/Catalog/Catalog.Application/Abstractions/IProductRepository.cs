using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchAsync(
        string? searchTerm,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? searchTerm, Guid? categoryId, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Remove(Product product);
}
