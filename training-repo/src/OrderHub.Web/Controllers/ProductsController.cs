using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private const int DefaultThreshold = 10;

    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(LowStockViewModel vm)
    {
        // threshold <= 0 或非數字 → DataAnnotations 讓 ModelState 失效 → 回表單顯示錯誤（非 500）
        if (!ModelState.IsValid)
            return View(vm);

        var threshold = vm.Threshold ?? DefaultThreshold;   // 未帶時預設 10
        vm.Threshold = threshold;             // 回填輸入框

        var items = await _productService.GetLowStockAsync(threshold);

        vm.Products = items.Select(x => new LowStockRowViewModel
        {
            Sku = x.Product.Sku,
            Name = x.Product.Name,
            StockQuantity = x.Product.StockQuantity,
            SoldLast30Days = x.SoldLast30Days
        }).ToList();

        return View(vm);
    }
}

