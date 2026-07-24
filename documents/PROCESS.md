# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：Claude Code（模型：Claude Opus 4.8，1M context）

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

- 開工前拆成四步：① 全庫掃描、讀 `documents/` 搞懂這份功課要做什麼 → ② 精讀 `training-repo` 原始碼（Services / Domain / Repositories / Controllers / DbSeeder）建立專案理解 → ③ 產出練習 1 的 agent 設定檔（`CLAUDE.md` + `.claude/`）→ ④ commit。
- 實際順序有變：本來想讀完就直接產設定檔，改成在第 ② 與第 ③ 步之間**先問兩個關鍵決策**——設定檔放 `training-repo/` 還是 repo 根目錄、要生成全套還是只要核心。原因：設定檔的**位置**會決定 hooks／權限在哪個 cwd 啟動 Claude Code 才生效，先問清楚比事後搬檔省事。

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

- 「讀懂陌生專案」這件事又快又好。當時的提問原文：
  > 帮我扫描下整个文件然后整理出这份功课是要做什么
  接著：
  > 帮我这里出项目的所有功能，然后生成所需要的文件。请记得任何关键性的问题，都先问我确认下
- 為什麼有效：第一句把 agent 當成「讀懂專案」的工具，讓它一次把散在 `documents/` 的四個練習、和 `training-repo` 的三層架構整併成一張地圖；第二句加了「**關鍵問題先確認**」這條護欄，逼它在動手前把決策點（放檔位置、生成範圍）攤出來問我，而不是自作主張產一堆要返工的檔。

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

- agent 一開始傾向直接沿用 `agent-configuration.md` 範例裡的 CLAUDE.md 文字，把建單流程講得比實際「乾淨」。我靠**逐檔讀原始碼**核對（`OrderService.cs`、`OrderRepository.cs`）才發現摘要抹掉了細節：`CreateOrderAsync` 其實是在同一個迴圈裡**邊驗證邊扣 `StockQuantity`**、把 per-line 錯誤累積進 `errors`，最後才統一裁決，而不是我描述的那種「先全部驗完再動手」。
- 教訓：agent（以及它引用的範例文件）給你的是**摘要**，摘要天生會抹掉邊界條件；要判斷對不對，得回到一手來源——程式碼、頁面、測試。

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

- 「**先計畫、再放行**」四步操作：
  1. 交任務時在 prompt 裡明講：「**任何關鍵決策先列出來問我，我確認前不要改任何檔案。**」
  2. 讓 agent 先回一份計畫：要動哪些檔、每檔職責、有哪些決策點。
  3. 我**逐條對照專案慣例**審這份計畫；看到「順手改 xxx / 一起重構 yyy」這種超出範圍的動作就要它拿掉。
  4. 只有審過的計畫才放行實作。
  （本次就是靠這招，在產出 7 個設定檔前先敲定「放 `training-repo/`、生成全套」兩個決策，沒有返工。）

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
   - **OrderHub.Web**：MVC 進入層。Controller（保持薄，只轉接 service 結果 + 手寫 ViewModel mapping）、Razor View（綁 ViewModel）、`Helpers/DisplayHelper`。
   - **OrderHub.Core**：商業邏輯核心。Domain models、Service 介面與實作（折扣、庫存、狀態轉移）、`Common`（`ServiceResult<T>`、`PagedResult<T>`）。**不依賴其他兩層**，Web 與 Infrastructure 都指向它。
   - **OrderHub.Infrastructure**：資料存取。`OrderHubDbContext`、Repositories（**唯一**碰 EF Core 的地方）、Migrations、`DbSeeder`。
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**
   - 找到的過度簡化：我把 `CreateOrderAsync` 描述成一條乾淨的「校驗閘門順序」。對照 `OrderService.cs` 才發現不精確——per-line 的錯誤（商品停售、庫存不足）是**先累積進 `errors` 清單**、而且**在同一個迴圈裡就先把 `product.StockQuantity` 減掉**，最後才用 `errors.Count > 0` 統一判定成敗；只是失敗時不呼叫 `SaveChangesAsync`，記憶體裡的扣減才不會落庫。「邊驗邊改、最後統一裁決」這個細節被順序式描述簡化掉了。
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方
   - 商業邏輯放 **Core 的 service**（透過 interface 注入）；只有 repository 碰 `DbContext`。
   - 新增一頁大致的落點：Controller（薄轉接）→ Core service 介面 + 實作（邏輯）→ repository（EF 查詢）→ ViewModel → View → `_Layout.cshtml` 導覽列連結 → tests。（正好是練習 3 的六個落點。）

> 補充（已實測）：`.claude` 的 hooks 確認運作——PostToolUse 每次 Edit/Write 都寫入 `.claude/hooks/edit-log.txt`；PreToolUse 對含 `TRUNCATE`／`DROP` 的指令回「Action denied」擋下。`dotnet test` 全程未跳權限確認（allow 規則生效）。`git push --force`（deny）與 `dotnet ef database drop`（ask）因不宜實際觸發未逐一觸發測試，規則設定已就位。

練習 2

> 這次採「**agent 定位 + 測試重現**」流程：沒有先在頁面上手動點，而是由 agent 從
> Controller 往下追到 Service／Repository 定位根因，並在動手修之前先寫一個
> 「修復前會紅、修復後才綠」的回歸測試來重現每個 bug；驗證靠 `dotnet test` 全綠
> 加 code-reviewer 複查；三個修復事後也已在頁面實測確認症狀消失。

### 排查過程（三個 bug）

**Bug 1 — 訂單列表分頁 off-by-one**（commit `785ff73`）

- 症狀：新建的訂單在第一頁找不到、要翻到後面；分頁最後一頁常常空白。
- 定位：`OrdersController.Index` → `OrderService.GetOrdersAsync` → `OrderRepository.GetPagedAsync`。
- 根因：`page` 是 1-based，卻用 `Skip(page * pageSize)`。page=1 時 Skip 掉最新一整頁
  （新訂單看不到），最後一頁 Skip 超過總筆數（空白）。
- 修法：改成 `Skip((page - 1) * pageSize)`（單行）。
- 回歸測試：`GetOrders_FirstPage_ReturnsNewestOrdersAndIsFull`（第一頁塞滿 20 筆、
  首筆為最新）、`GetOrders_LastPage_IsNotEmpty`（25 筆時第 2 頁應有 5 筆）。修復前紅、修復後綠。

**Bug 2 — Gold 會員總額雙重折扣**（commit `66f59d4`）

- 症狀：Gold 會員新訂單應付總額比手算少一截（原價 1000 × 1 顯示 810，應為 900），Silver 正常。
- 定位：`OrderService.CreateOrderAsync` 建單邏輯 + `CalculateTotal`。
- 根因：CreateOrderAsync 針對 Gold 先把折扣套進 `UnitPriceSnapshot`（存成 900），
  `CalculateTotal` 又在總額上再折一次（×0.9），Gold 變成 0.9×0.9=0.81；Silver 沒這段特例。
  也違反「會員折扣在訂單總額上折抵一次」的領域規則。
- 修法：移除 Gold 特例，snapshot 一律存下單當下原價，折扣統一由 `CalculateTotal` 套一次；
  三種等級走同一條路徑。
- 回歸測試：`CreateOrder_GoldCustomer_SnapshotsOriginalPriceAndDiscountsTotalOnce`
  （驗 snapshot=1000 且總額=900）。修復前紅、修復後綠。

**Bug 3 — 取消訂單未加回庫存**（commit `bb2afc5`）

- 症狀：庫存跟盤點對不上，反覆「建單 → 取消」後庫存只減不還。
- 定位：`OrderService.CancelOrderAsync`。
- 根因：先執行 `order.Status = Cancelled`，接著才判斷 `if (Status == Pending || Confirmed)`
  決定是否加回庫存——狀態既已被改成 Cancelled，該判斷永遠為 false，加回庫存的迴圈從不執行。
- 修法：把 `order.Status = Cancelled` 移到加回庫存迴圈之後，讓判斷依「取消前」狀態評估。
- 回歸測試：`CancelOrder_ActiveOrder_RestoresProductStock`（Theory：Pending / Confirmed，
  建單扣庫存 10→7、取消後應加回 10）。修復前紅、修復後綠。

### 自我驗證

1. **重現方式**：這次沒有先在頁面手動重現，而是定位根因後**先寫一個「修復前會紅」的
   回歸測試**來重現每個 bug（三個都確認修復前紅、修復後綠）。事後也已在頁面確認修復後症狀消失。
2. **給 agent 的是具體現象而非客訴原文**：頁碼（第一頁／最後一頁）、金額（1000 → 應 900、
   實顯 810）、庫存數字（10 → 7 → 應回 10）。
3. **症狀消失的驗證**：以回歸測試（紅 → 綠）＋ code-reviewer 複查確認，並已回頁面實測確認症狀消失。
4. 每個 bug 都補了回歸測試，`dotnet test` 全綠（**33/33**）。✅
5. 三個獨立 commit（`785ff73` / `66f59d4` / `bb2afc5`），message 用「症狀 → 根因 → 修法」
   格式，各自只含原始碼 ＋ 對應測試兩檔。✅
6. **（思考題）為什麼原本的測試沒抓到這三個 bug？**
   共同原因：**既有測試只斷言「最顯眼的主要輸出」，沒覆蓋 bug 所在的次要效果與整合路徑。**
   - **Bug 1**：`GetOrders_ReportsTotalCountAndTotalPages` 只驗 `TotalCount`／`TotalPages`，
     這兩個值來自 `CountAsync()` 與公式、**與 `Skip` 無關**，所以分頁算錯也照過；而
     `GetOrders_WithStatusFilter` 用 `Assert.All(result.Items, …)`——**空集合上 `Assert.All`
     恆為真**，bug 讓該頁回空反而讓斷言「假通過」。沒有任何測試斷言「某一頁實際有哪些／幾筆」。
   - **Bug 2**：`CalculateTotal_AppliesTierDiscountOnSubtotal` 是用**手動塞好原價 snapshot**
     的 Order 單獨測 `CalculateTotal`；`CreateOrder_SnapshotsCurrentUnitPrice` 用的是
     **Standard 客戶**（打不到 Gold 特例）。兩半各自測都對，但沒有測「CreateOrderAsync + Gold
     + CalculateTotal」串起來的整合路徑，雙重折扣就從縫隙漏掉。
   - **Bug 3**：`CancelOrder_ActiveOrder_SetsStatusCancelled` 只斷言結果 `Status == Cancelled`，
     **從沒檢查取消後的 `Product.StockQuantity`**——庫存還原這個副作用完全在測試視野之外。
   - 一句話：測試測了「該回傳什麼」，卻沒測「順帶改了什麼、整條路走完對不對」，
     bug 就活在**沒被斷言的維度**裡。

練習 3

> 這題用「**先計畫、你核准後才實作**」的 Plan Mode 流程做，並在功能完成後**起真實網站
> （localhost:5150 + 本機 SQL Server）逐條頁面實測**，不是只靠測試。

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
   - ✅ 真實頁面實測：不帶參數時輸入框預設 `value="10"`、列出庫存 < 10 的商品；
     `?threshold=3` 結果隨門檻改變（curl 打端點皆 HTTP 200）。
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
   - ✅ `0`、`-1`、甚至 `abc`（非數字）都回 **HTTP 200（非 500）**，顯示驗證訊息
     「門檻必須大於 0」，且驗證失敗時表格隱藏。機制：`LowStockViewModel.Threshold`
     用 `int? + [Range(1, int.MaxValue)]`，controller `!ModelState.IsValid` 回表單。
   - ⚠️ 教訓：我第一版用 grep 檢查頁面時誤報「驗證訊息不見」，實際是 Razor 把中文輸出成
     HTML 實體（`&#x9580;…`=門），逐字檢視 HTML 才確認訊息有正常渲染——再次印證「agent
     的話（含它自己寫的檢查腳本）要回一手來源人工核對」。
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
   - ✅ 測試 `GetLowStock_SoldLast30Days_ExcludesCancelledAndOlderThan30Days`：
     -10 天/Confirmed/qty5（計入）、-10 天/Cancelled/qty7（排除）、-40 天/Confirmed/qty9
     （排除，超過 30 天）、-2 天/Shipped/qty3（計入）→ 售出量 = 8。
4. 停售（已停售 badge）商品不出現在列表
   - ✅ 測試 `GetLowStock_ExcludesInactiveProducts`（active stock4 出現、inactive stock2
     不出現）；repository 用 `Where(p => p.IsActive && p.StockQuantity < threshold)`。
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
   - ✅ EF 只在 repository、「近 30 天」業務窗口在 Core service、Controller 薄轉接 +
     ViewModel mapping、View 綁 ViewModel、驗證用 DataAnnotations。經 code-reviewer 複查
     通過（無阻擋問題），我逐項確認；並依 review 建議修掉「驗證失敗仍顯示空表提示」的
     誤導、把門檻預設抽成 `DefaultThreshold` 常數。
6. 至少 3 個新測試，`dotnet test` 全綠
   - ✅ 服務層 4 個（門檻過濾+升冪、排除停售、近 30 天排除 Cancelled、無銷售回 0）+
     驗證層（ViewModel `[Range]` Theory 5 案例 + controller 2 個）；**44/44 全綠**。
   - commit：`bf96f59`（功能）、`5d86c9c`（threshold 驗證測試）。

練習 4

> 這題同樣「**先出計畫、你核准才動手**」（Plan Mode），且是**行為不變的重構**。

1. 重構後 `dotnet test` 全綠
   - ✅ 重構後 **44/44 全綠**（沿用重構前同一組測試，是行為不變的主要證據）。
2. 我能說出這次重構「改善了什麼、沒有改變什麼」
   - **改善了什麼**：`CreateOrderAsync` 原本把「前置整體驗證」與「逐項處理」全塞在同一個
     方法，又長又難讀；抽成兩個具名私有方法——`ValidateRequest`（static 純函式，前置
     四項驗證、短路只回第一個錯誤）與 `BuildOrderItemsAsync`（逐項驗證商品/庫存、扣庫存、
     加明細、回累積錯誤），主方法瘦成「驗證 → 建立 → 存檔」骨架。
   - **沒有改變什麼**：對外行為、四則錯誤訊息文字、錯誤累積順序、副作用時機（扣庫存與加
     明細在同一 pass）、「失敗不呼叫 SaveChanges 故不落庫」全部原樣保留；未動
     `CancelOrderAsync`、折扣方法或其他層。
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）
   - ✅ 重構前先出計畫核准；完成後用 code-reviewer 對 `HEAD` **逐行比對**確認等價（前置
     四項檢查逐字相同、逐項迴圈 byte-for-byte、`customer!` 由 ValidateRequest 的 null
     檢查保證安全），我也自己看過 diff 才 commit，不是只聽 agent 說好。
   - commit：`155cb51`。

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

- **我怎麼問**：「出项目的所有功能，然后生成所需要的文件。请记得任何关键性的问题，都先问我确认下。」
- **它怎麼答（摘要）**：先把 OrderHub 的三層職責、領域模型、7 個功能頁與商業規則整理成一張地圖；接著**停下來**用兩個選擇題問我「設定檔放 `training-repo/` 還是 repo 根目錄」「生成全套還是只要核心」；等我選完（放 `training-repo/`、全套）才動手產出 `CLAUDE.md` + `.claude/`（settings 權限與 hooks／code-reviewer 與 test-runner 子代理／fix-bug skill／兩個 hook 腳本）共 7 個檔並 commit。
- **值得記住的點**：把「關鍵問題先確認」寫進 prompt，agent 就會在動手前把決策攤開來問，而不是先斬後奏產一堆要返工的檔。
