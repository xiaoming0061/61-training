using OrderHub.Core.Domain;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 8, sku: "SKU-S8");
        TestSetup.AddProduct(db, stock: 3, sku: "SKU-S3");
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-S10"); // 邊界：不 < 10，應排除
        TestSetup.AddProduct(db, stock: 12, sku: "SKU-S12");

        var result = await service.GetLowStockAsync(10);

        // 只回庫存 < 10 的兩筆，且依庫存升冪 [3, 8]
        Assert.Equal(new[] { 3, 8 }, result.Select(r => r.Product.StockQuantity).ToArray());
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var active = TestSetup.AddProduct(db, stock: 4, isActive: true, sku: "SKU-ACT");
        TestSetup.AddProduct(db, stock: 2, isActive: false, sku: "SKU-INACT"); // 停售，即使庫存更低也不列入

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Product.Id);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledAndOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 3, sku: "SKU-SOLD");

        AddOrder(db, customer.Id, DateTime.UtcNow.AddDays(-10), OrderStatus.Confirmed, product.Id, 5); // 計入
        AddOrder(db, customer.Id, DateTime.UtcNow.AddDays(-10), OrderStatus.Cancelled, product.Id, 7); // 排除：Cancelled
        AddOrder(db, customer.Id, DateTime.UtcNow.AddDays(-40), OrderStatus.Confirmed, product.Id, 9); // 排除：超過 30 天
        AddOrder(db, customer.Id, DateTime.UtcNow.AddDays(-2), OrderStatus.Shipped, product.Id, 3);    // 計入

        var result = await service.GetLowStockAsync(10);

        var row = Assert.Single(result);
        Assert.Equal(8, row.SoldLast30Days); // 5 + 3
    }

    [Fact]
    public async Task GetLowStock_ProductWithNoSales_ReportsZero()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 4, sku: "SKU-NOSALE");

        var result = await service.GetLowStockAsync(10);

        var row = Assert.Single(result);
        Assert.Equal(0, row.SoldLast30Days);
    }

    private static void AddOrder(
        OrderHubDbContext db, int customerId, DateTime createdAt, OrderStatus status, int productId, int quantity)
    {
        db.Orders.Add(new Order
        {
            CustomerId = customerId,
            Status = status,
            CreatedAt = createdAt,
            Items =
            {
                new OrderItem { ProductId = productId, Quantity = quantity, UnitPriceSnapshot = 100m }
            }
        });
        db.SaveChanges();
    }
}
