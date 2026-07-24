# OrderHub — 專案記憶

## 專案簡介

公司內部訂單管理系統：業務可建立／查詢訂單、管理商品與客戶。
內部使用、單一 SQL Server 資料庫，不需考慮多租戶、高併發或微服務架構。

## 技術棧

- .NET 8 / ASP.NET Core MVC（Razor Views + Bootstrap 5，前端資源皆為本地檔案，不依賴 CDN）
- EF Core 8 + SQL Server（本機安裝，不使用 Docker）
- 測試：xUnit + EF Core InMemory（**不需要** SQL Server，也不會動到你的資料庫）

## 分層與慣例

三層架構，相依方向 `Web → Core ← Infrastructure`：

- `OrderHub.Web`：Controller / View / ViewModel / `Helpers/DisplayHelper`
- `OrderHub.Core`：Domain / Services（介面 + 商業邏輯）/ Common（`ServiceResult<T>`、`PagedResult<T>`）
- `OrderHub.Infrastructure`：`OrderHubDbContext` / Repositories / Migrations / `DbSeeder`

慣例（新增或修改功能時請遵循）：

- Controller 保持薄，只轉接 service 結果並做 ViewModel mapping；商業邏輯一律放 Core 的 service，透過 interface 注入
- 只有 repository 碰 `DbContext`；Controller / Service **不可**直接使用 EF Core
- Service 用 `ServiceResult<T>` 表達預期內的失敗（找不到客戶、庫存不足…），**不要用丟例外**的方式表達
- View 綁 ViewModel（手寫 mapping），不要把 domain model 直接丟給 View
- 使用者輸入用 DataAnnotations + ModelState 驗證；輸入錯誤要回表單顯示，**絕不能變成 500**
- 金額一律用 `decimal`（DB 精度 18,2）；折扣集中在 `OrderService`（`GetDiscountRate` / `CalculateSubtotal` / `CalculateTotal`），不要在別處重算
- 操作結果訊息用 `TempData["Success"] / TempData["Error"]`（`_Layout.cshtml` 有共用 alert 區塊）
- 參考檔：Controller 照 `ProductsController.cs`、Service 照 `ProductService.cs`、測試照 `tests/OrderHub.Tests/*` 的寫法

## 領域規則（重點）

- 會員等級 `CustomerTier`：Standard 不打折、Silver 95 折（5%）、Gold 9 折（10%），在**訂單總額上折抵一次**
- 訂單狀態 `OrderStatus`：Pending → Confirmed → Shipped；僅 Pending／Confirmed 可取消（轉 Cancelled）
- 建單會扣庫存並快照當下單價（`OrderItem.UnitPriceSnapshot`）；取消訂單要把庫存加回

## 常用指令

- `dotnet build`：建置
- `dotnet test`：跑全部測試（InMemory，不需 SQL Server）
- `dotnet run --project src/OrderHub.Web`：啟動網站（http://localhost:5150）

## 重要／危險檔案

- `src/OrderHub.Infrastructure/Migrations/**`：EF migration 是歷史紀錄，**不要手改**
- `src/OrderHub.Web/appsettings*.json`：連線字串等設定，改動前先問

## 不要做的事

- 不要未經同意就加新的 NuGet 套件
- 不要在 Controller / Service 直接使用 `DbContext`
- 不要為了「順手」重構與當前任務無關的程式碼
- 不要讀取或寫入任何機密檔（*.pfx、appsettings.Production.json、user-secrets）
