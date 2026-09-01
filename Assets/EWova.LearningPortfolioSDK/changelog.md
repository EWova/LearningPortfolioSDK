# Changelog
## [2026.8.5] - 2026-09-01
### Changed
- **(Breaking)** `[Column]` 欄位若為日期時間，請改用 `DateTimeOffset`（不再支援 `DateTime`，可保留明確的 UTC offset）
- **(Breaking)** `SheetHelper` 遇到不支援的欄位型別時，現在會直接拋出例外，不再靜默轉換成不可逆的格式
## [2026.8.4] - 2026-08-28
### Added
- `SheetHelper` 新增 `WarmUp` / `Release`，可手動控制欄位對應快取的預熱與釋放時機
- `SheetHelper` 新增 `CreateFrom<T>` / `CreateFromRow<T>`，直接配置新物件並讀取資料
- `SheetHelper` 的 Enum 欄位現在也支援可逆序列化
- 新增 `SheetHelperExtensions`，`AlignToColumns` / `ReadFrom` / `ReadFromRow` / `CreateFrom` / `CreateFromRow` 可改用物件導向風格呼叫（例如 `record.AlignToColumns(page)`）
- `UserProjectRecordSheet` 新增 `IsProgressCompleted` / `IsProgressMarked` / `IsProgressNodeCompleted` / `IsProgressNodeMarked`，不用再自行分辨「原始標記」與「含父子關係的完成」
- `EWovaLoginPlane` 新增 `OnGameStartWithUserData` 事件，可直接取得登入的使用者資料
- `NetServiceAsyncRespond` / `NetServiceAsyncRespond<T>` 新增 `Status`（`AsyncRespondStatus`）欄位
### Changed
- **(Breaking)** `SheetHelper.WriteToRow(object, Row)` 改為 `SheetHelper.AlignToColumns(object, Page)`：不再需要既有的 Row，改依 `Page` 欄位順序組出陣列，供 `AddRowAndSetCells` 新增一列使用
- **(Breaking)** Progress 相關 API 重新命名以統一語意：
  - `ProgressNode.SetComplete` → `SetMark`、`IsCompletedSelf` → `IsMarked`、`CompleteTime` → `MarkedTime`（舊名稱保留為 `[Obsolete]` 別名）
  - `Sheet.SetCompleteIncludeNonNode` / `SetUnmarkIncludeNonNode` → `SetProgressMark` / `SetProgressUnmark`（**無**別名，需自行改名）
  - `Sheet.ProgressCompletionDic` → `AllMarkedProgressDic`（`ProgressCompletions` / `ProgressCompletionsLocalDateTime` 仍保留為 `[Obsolete]` 別名）
- **(Breaking)** `LearningPortfolio.CreateUserProjectRecordShower` 改名為 `CreateUserProjectSheetShower`，並在未連線（`IsConnected == false`）時直接拋出例外（舊名稱保留為 `[Obsolete]` 別名）
- **(Breaking)** `sheet.IsAnyNetSerivceRequesting`（拼字錯誤）改名為 `IsAnyNetServiceRequesting`，**無**別名
- **(Breaking)** `RequestAsync` 的結果狀態從三態（`Success` / `Failed` / `Exception`）簡化為兩態：可預期的後端/業務錯誤仍回傳 `.IsFailed`；**其餘未預期的例外與取消現在會直接從 `RequestAsync()` 拋出**，不再包裝進結果內。Callback 風格的 `.Request(...)` 行為不變
- 移除 BasicAssets 範例內建的 DeepLink 登入流程
- 修正多處拼字錯誤：`bloackedMsg` → `blockedMsg`、`DisableOriginCloseButtonBehaviour` → `DisableOriginCloseButtonBehavior`
### Fixed
- 修正 `ClearReadableData` 清除頁面資料後，欄位總結未重新計算、殘留舊值的問題
- 修正頁面沒有欄位、或後台回傳空欄位總結時，總結未被清空、殘留錯誤舊值的問題
- 修正取消網路請求（`CancelAll` / `Dispose`）時，呼叫端的 `await` 可能永遠停留在 pending、收不到結果的問題
- 移除 BasicAssets 範例內建的 `LearningPortfolioProfile.asset`，避免重新匯入範例時覆寫並遺失專案已設定的 APIKey
## [2026.8.3] - 2026-07-31
### Added
- 新增 `LearningPortfolio.ConnectBlocker`，可自訂連線前置檢查邏輯（例如遊戲進行中不允許連線）
- 新增 `LearningPortfolio.ConnectedProject`，可取得目前已連線的專案資訊
- Unity Editor 匯入套件後新增歡迎視窗，提供版本資訊與快速入門連結
- 使用 DeepLink 登入時，等待逾 8 秒顯示「PC Link」連線提示（XR 環境）；超過 60 秒自動取消並顯示逾時訊息
- `ConnectResponse` / `CheckAvailabilityResponse` 新增 `ClientErrorMessage` 欄位，提供可直接顯示給使用者的本地端錯誤說明
### Changed
- **(Breaking)** `ApiSettings` 重新命名為 `ProjectSettings`；`LearningPortfolioProfile.APISettings` 欄位改名為 `ProjectSettings`（Inspector 既有設定不會遺失，但直接以程式碼參照 `ApiSettings` / `APISettings` 的專案需自行更新）
- `LPApiClient.GetTex2D` 新增 `IProgress<float>` 參數，可回報圖片下載進度
### Fixed
- 修正 IL2CPP 在較高 Managed Stripping Level 下，可能裁剪 API 回應 Model 導致打包後反序列化失敗或欄位遺失的問題
- 修正 `LearningPortfolioApiException.Message` 遺失原始例外訊息的問題
- 連線與取得學習歷程失敗時，現在會正確填入 `Exception` / `ClientErrorMessage` / `ServerErrorMessage`
- `ConnectAsync` 加入重入保護，避免併發呼叫造成資源洩漏
- 修正取得使用者紀錄清單為空、或首頁欄位數不足時直接丟出例外的問題，改為明確例外或記錄警告後略過
- 使用者紀錄圖示改為逐張下載並各自 try/catch，單張下載失敗不再中斷整個學習歷程載入流程
- 修正併發呼叫 `ConnectAsync` 時可能回傳錯誤結果的問題
## [1.3.0] - 2026-05-06
- 將 Core 解耦為獨立套件
## [1.2.2] - 2026-04-13
- 修正一體機在使用學習歷程輸入帳號密碼時，插入符無法使用問題 [Github](https://github.com/EWova/LearningPortfolioSDK/releases/tag/v1.2.2)
## [1.2.1] - 2026-03-04
- 修正使用裝置時間不正確皆為 0
## [1.2.0] - 2026-02-24
- 將 SDK 上傳到 Github 並可被作為 PackageManager 直接拉取 ([Github](https://github.com/EWova/LearningPortfolioSDK))
- 修改 `Deeplink` 的 `Scheme` 定義方法
## [1.1.0] - 2025-10-10
### Added
- 新增 `SheetHelper` 與 `ColumnAttribute` 協助欄位序列化 ([範例](/LearningPortfolio/Tutorial#詳細資料))
- 新增 以學習旗幟類遊戲成就系統方式新增了([範例](/LearningPortfolio/Tutorial#進度節點))
  - `ProgressNode` 進度旗幟
  - `ProgressCompletions` 完成的進度
  - `ProgressCompletionsLocalDateTime` 完成的時間
### Changed
- 移除 `Sheet/CompletionProgress` 數值修改，使其為唯獨
- 簡化了 `LearningPortfolio.Instance.屬性` 調用，可直接呼叫`LearningPortfolio.屬性`
- 將 `Page/AddRow` 替換為 `Page/AddRowAndSetData` 
  - 原本需要使用不直覺的方式處理
    1. 加行列 `Page/AddRow`
    2. 再編輯該列 `Row/EditCell`
    
  - 現在只需要在加列同時把數值塞入就可以了
    1. `Page/AddRowAndSetCells` 
### Fixed
- 修復 對 Unity 舊版本支援 (2021.3)
- 修復 throw Exception 無法追蹤問題
## [1.0] - 2025-07-31
  - 初始版本發布