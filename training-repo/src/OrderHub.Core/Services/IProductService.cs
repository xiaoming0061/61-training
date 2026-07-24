using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();

    /// <summary>販售中且庫存低於門檻的商品（依庫存升冪），附近 30 天售出數量（排除 Cancelled）。</summary>
    Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold);
}
