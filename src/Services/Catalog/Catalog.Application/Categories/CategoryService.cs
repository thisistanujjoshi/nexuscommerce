using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Domain.Entities;

namespace Catalog.Application.Categories;

public class CategoryService(
    ICategoryRepository categories,
    IProductRepository products,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await categories.GetAllAsync(ct);
        return all.Select(CategoryDto.FromEntity).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        return CategoryDto.FromEntity(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category(request.Name, request.Description);
        await categories.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CategoryDto.FromEntity(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        category.Rename(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(ct);
        return CategoryDto.FromEntity(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var productsInCategory = await products.CountAsync(null, id, ct);
        if (productsInCategory > 0)
            throw new ConflictException(
                $"Cannot delete category '{category.Name}' while it still contains {productsInCategory} product(s).");

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
