using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Domain.Entities;

namespace Catalog.Application.Products;

public class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return ProductDto.FromEntity(product);
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(
        string? searchTerm, Guid? categoryId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await products.SearchAsync(searchTerm, categoryId, page, pageSize, ct);
        var total = await products.CountAsync(searchTerm, categoryId, ct);

        return new PagedResult<ProductDto>(
            items.Select(ProductDto.FromEntity).ToList(), page, pageSize, total);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (!await categories.ExistsAsync(request.CategoryId, ct))
            throw new NotFoundException(nameof(Category), request.CategoryId);

        var existing = await products.GetBySkuAsync(request.Sku, ct);
        if (existing is not null)
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");

        var product = new Product(
            request.Sku, request.Name, request.Description,
            request.Price, request.StockQuantity, request.CategoryId);

        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        if (!await categories.ExistsAsync(request.CategoryId, ct))
            throw new NotFoundException(nameof(Category), request.CategoryId);

        product.UpdateDetails(request.Name, request.Description, request.CategoryId);
        await unitOfWork.SaveChangesAsync(ct);

        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto> ChangePriceAsync(Guid id, ChangePriceRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.ChangePrice(request.Price);
        await unitOfWork.SaveChangesAsync(ct);

        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.AdjustStock(request.Delta);
        await unitOfWork.SaveChangesAsync(ct);

        return ProductDto.FromEntity(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
