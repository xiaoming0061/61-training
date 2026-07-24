using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

/// <summary>低庫存查詢結果：商品 + 近 30 天售出數量（排除 Cancelled 訂單）。</summary>
public record LowStockProduct(Product Product, int SoldLast30Days);
