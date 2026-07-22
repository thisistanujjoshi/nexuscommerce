using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Xunit;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    private static Product NewProduct(int stock = 10, decimal price = 9.99m) =>
        new("sku-123", "Test Product", "A product for testing", price, stock, Guid.NewGuid());

    [Fact]
    public void Constructor_NormalizesSku_ToUpperInvariant()
    {
        var product = NewProduct();

        Assert.Equal("SKU-123", product.Sku);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankSku_Throws(string sku)
    {
        Assert.Throws<DomainException>(() =>
            new Product(sku, "Name", "Desc", 1m, 1, Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string name)
    {
        Assert.Throws<DomainException>(() =>
            new Product("SKU-1", name, "Desc", 1m, 1, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithNegativePrice_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new Product("SKU-1", "Name", "Desc", -0.01m, 1, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithNegativeStock_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new Product("SKU-1", "Name", "Desc", 1m, -1, Guid.NewGuid()));
    }

    [Fact]
    public void AdjustStock_WithPositiveDelta_IncreasesQuantity()
    {
        var product = NewProduct(stock: 10);

        product.AdjustStock(5);

        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_WithNegativeDelta_DecreasesQuantity()
    {
        var product = NewProduct(stock: 10);

        product.AdjustStock(-10);

        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_BelowZero_Throws()
    {
        var product = NewProduct(stock: 3);

        Assert.Throws<DomainException>(() => product.AdjustStock(-4));
        Assert.Equal(3, product.StockQuantity);
    }

    [Fact]
    public void ChangePrice_WithNegativePrice_Throws()
    {
        var product = NewProduct(price: 10m);

        Assert.Throws<DomainException>(() => product.ChangePrice(-1m));
        Assert.Equal(10m, product.Price);
    }

    [Fact]
    public void ChangePrice_SetsUpdatedTimestamp()
    {
        var product = NewProduct();
        Assert.Null(product.UpdatedAtUtc);

        product.ChangePrice(12.34m);

        Assert.Equal(12.34m, product.Price);
        Assert.NotNull(product.UpdatedAtUtc);
    }
}
