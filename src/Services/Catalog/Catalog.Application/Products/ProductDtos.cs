using Catalog.Domain.Entities;

namespace Catalog.Application.Products;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static ProductDto FromEntity(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.CategoryId,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);
}

public record CreateProductRequest(
    string Sku,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId);

public record UpdateProductRequest(
    string Name,
    string Description,
    Guid CategoryId);

public record ChangePriceRequest(decimal Price);

public record AdjustStockRequest(int Delta);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
