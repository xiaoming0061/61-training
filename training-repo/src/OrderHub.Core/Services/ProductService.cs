using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold)
    {
        // 「近 30 天」這條業務規則在 service 決定；EF 查詢留在 repository。
        var since = DateTime.UtcNow.AddDays(-30);
        var products = await _productRepository.GetLowStockActiveAsync(threshold);
        var soldMap = await _productRepository.GetSoldQuantitiesSinceAsync(since);

        return products
            .Select(p => new LowStockProduct(p, soldMap.TryGetValue(p.Id, out var sold) ? sold : 0))
            .ToList();
    }
}
