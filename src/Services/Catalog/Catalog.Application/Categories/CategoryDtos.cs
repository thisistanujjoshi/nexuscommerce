using Catalog.Domain.Entities;

namespace Catalog.Application.Categories;

public record CategoryDto(Guid Id, string Name, string Description)
{
    public static CategoryDto FromEntity(Category category) =>
        new(category.Id, category.Name, category.Description);
}

public record CreateCategoryRequest(string Name, string Description);

public record UpdateCategoryRequest(string Name, string Description);
