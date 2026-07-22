using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // EF Core materialization
    private Product()
    {
        Sku = null!;
        Name = null!;
        Description = null!;
    }

    public Product(string sku, string name, string description, decimal price, int stockQuantity, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");
        if (price < 0)
            throw new DomainException("Price cannot be negative.");
        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        Id = Guid.NewGuid();
        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        CategoryId = categoryId;
        Touch();
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new DomainException("Price cannot be negative.");

        Price = newPrice;
        Touch();
    }

    public void AdjustStock(int delta)
    {
        var newQuantity = StockQuantity + delta;
        if (newQuantity < 0)
            throw new DomainException($"Cannot reduce stock below zero (current: {StockQuantity}, requested change: {delta}).");

        StockQuantity = newQuantity;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
