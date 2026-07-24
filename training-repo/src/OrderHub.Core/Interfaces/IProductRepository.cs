using OrderHub.Core.Domain;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<Product?> GetByIdAsync(int id);

    /// <summary>販售中且庫存低於門檻的商品，依庫存量升冪。</summary>
    Task<IReadOnlyList<Product>> GetLowStockActiveAsync(int threshold);

    /// <summary>自指定時間起、排除 Cancelled 的各商品售出總量（ProductId → 售出數量）。</summary>
    Task<IReadOnlyDictionary<int, int>> GetSoldQuantitiesSinceAsync(DateTime since);

    Task SaveChangesAsync();
}
