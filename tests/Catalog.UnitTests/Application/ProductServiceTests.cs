using Catalog.Application.Abstractions;
using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Entities;
using Moq;
using Xunit;

namespace Catalog.UnitTests.Application;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_products.Object, _categories.Object, _unitOfWork.Object);
    }

    private static Product ExistingProduct() =>
        new("SKU-1", "Existing", "Desc", 10m, 5, Guid.NewGuid());

    [Fact]
    public async Task GetById_WhenMissing_ThrowsNotFound()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_WithUnknownCategory_ThrowsNotFound()
    {
        _categories.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateProductRequest("SKU-9", "New", "Desc", 5m, 1, Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request));
        _products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_ThrowsConflict()
    {
        _categories.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _products.Setup(r => r.GetBySkuAsync("SKU-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingProduct());

        var request = new CreateProductRequest("SKU-1", "New", "Desc", 5m, 1, Guid.NewGuid());

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithValidRequest_PersistsAndReturnsDto()
    {
        _categories.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _products.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest("sku-9", "New Product", "Desc", 5m, 3, categoryId);

        var dto = await _sut.CreateAsync(request);

        Assert.Equal("SKU-9", dto.Sku);
        Assert.Equal("New Product", dto.Name);
        Assert.Equal(categoryId, dto.CategoryId);
        _products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdjustStock_PersistsChange()
    {
        var product = ExistingProduct();
        _products.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var dto = await _sut.AdjustStockAsync(product.Id, new AdjustStockRequest(-2));

        Assert.Equal(3, dto.StockQuantity);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_ClampsPageAndPageSize()
    {
        _products.Setup(r => r.SearchAsync(null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _products.Setup(r => r.CountAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.SearchAsync(null, null, page: -5, pageSize: 5000);

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        _products.Verify(r => r.SearchAsync(null, null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenMissing_ThrowsNotFound()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
        _products.Verify(r => r.Remove(It.IsAny<Product>()), Times.Never);
    }
}
