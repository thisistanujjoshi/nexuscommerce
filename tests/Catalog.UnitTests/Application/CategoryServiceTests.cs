using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Catalog.Application.Common;
using Catalog.Domain.Entities;
using Moq;
using Xunit;

namespace Catalog.UnitTests.Application;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categories.Object, _products.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Create_PersistsAndReturnsDto()
    {
        var dto = await _sut.CreateAsync(new CreateCategoryRequest("  Books  ", "All books"));

        Assert.Equal("Books", dto.Name);
        _categories.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenCategoryHasProducts_ThrowsConflict()
    {
        var category = new Category("Books", "All books");
        _categories.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _products.Setup(r => r.CountAsync(null, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(category.Id));
        _categories.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenEmpty_RemovesAndSaves()
    {
        var category = new Category("Books", "All books");
        _categories.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _products.Setup(r => r.CountAsync(null, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.DeleteAsync(category.Id);

        _categories.Verify(r => r.Remove(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenMissing_ThrowsNotFound()
    {
        _categories.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }
}
