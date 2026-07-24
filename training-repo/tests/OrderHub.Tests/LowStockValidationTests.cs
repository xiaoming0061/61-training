using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Web.Controllers;
using OrderHub.Web.ViewModels;

namespace OrderHub.Tests;

/// <summary>
/// 守護「threshold 驗證」這條規格：未帶預設、&lt;=0／非數字要走驗證錯誤而非 500。
/// ViewModel 層直接驗 DataAnnotations；Controller 層驗 ModelState 失效時的行為。
/// </summary>
public class LowStockValidationTests
{
    [Theory]
    [InlineData(null, true)]  // 未帶 → 有效（預設 10 由 controller 決定）
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(0, false)]    // <= 0 → 無效
    [InlineData(-1, false)]
    public void LowStockViewModel_ThresholdRange_ValidatesCorrectly(int? threshold, bool expectedValid)
    {
        var vm = new LowStockViewModel { Threshold = threshold };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            vm, new ValidationContext(vm), results, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task LowStock_InvalidModelState_ReturnsViewAndDoesNotQuery()
    {
        using var db = TestSetup.CreateContext();
        TestSetup.AddProduct(db, stock: 4, sku: "SKU-LOW"); // 即使有低庫存商品也不該被查出來
        var controller = new ProductsController(TestSetup.CreateProductService(db));
        controller.ModelState.AddModelError(nameof(LowStockViewModel.Threshold), "門檻必須大於 0");

        var result = await controller.LowStock(new LowStockViewModel { Threshold = 0 });

        // 回表單顯示錯誤（ViewResult），沒有查詢、沒有 500 例外
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LowStockViewModel>(view.Model);
        Assert.Empty(model.Products);
    }

    [Fact]
    public async Task LowStock_NoThreshold_DefaultsTo10AndReturnsMatchingProducts()
    {
        using var db = TestSetup.CreateContext();
        TestSetup.AddProduct(db, stock: 4, sku: "SKU-LOW");   // < 10 → 出現
        TestSetup.AddProduct(db, stock: 50, sku: "SKU-HIGH"); // >= 10 → 不出現
        var controller = new ProductsController(TestSetup.CreateProductService(db));

        var result = await controller.LowStock(new LowStockViewModel()); // Threshold 未帶

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LowStockViewModel>(view.Model);
        Assert.Equal(10, model.Threshold);       // 預設 10 並回填輸入框
        Assert.Single(model.Products);
        Assert.Equal("SKU-LOW", model.Products[0].Sku);
    }
}
