# Changelog
## [2026.8.4] - 2026-08-28
### Added
- `SheetHelper` 新增 `WarmUp<T>()` / `WarmUp(params Type[])` 與 `Release<T>()` / `Release(params Type[])` / `ReleaseAll()`，可手動控制欄位對應反射快取的預熱與釋放時機
- `SheetHelper` 新增 `CreateFrom<T>` / `CreateFromRow<T>`，可直接配置新物件並讀取資料（`ReadFrom` / `ReadFromRow` 則是重複利用既有物件、零額外配置的寫法）
- `SheetHelper` 的 Enum 欄位現在也支援可逆序列化（寫入用 `ToString()`，讀回用 `Enum.Parse`）
- 新增 `SheetHelperExtensions`，`WriteTo` / `AlignToColumns` / `ReadFrom` / `ReadFromRow` / `CreateFrom` / `CreateFromRow` 都可改用物件導向風格呼叫（例如 `record.AlignToColumns(page)`）
- `UserProjectRecordSheet` 新增 `IsProgressCompleted` / `IsProgressMarked` / `IsProgressNodeCompleted` / `IsProgressNodeMarked`，呼叫端不用再自行分辨「原始標記」與「含父子關係的完成」兩種語意
- `EWovaLoginPlane` 新增 `OnGameStartWithUserData` 事件，可直接取得登入的使用者資料
- `NetServiceAsyncRespond` / `NetServiceAsyncRespond<T>` 新增 public 的 `Status`（`AsyncRespondStatus` enum）欄位
- `package.json` 補上 `documentationUrl` / `changelogUrl` / `license` / `author` 等中繼資料
### Changed
- **(Breaking)** `SheetHelper.WriteToRow(object, LearningPortfolio.Row)` 改為 `SheetHelper.AlignToColumns(object, LearningPortfolio.Page)`：不再需要既有的 Row，改依 `Page` 的欄位順序組出陣列，專供 `AddRowAndSetCells` 新增一列使用；`WriteTo` / `AlignToColumns` 也移除了誤用的 `in` 參數修飾詞
- **(Breaking)** Progress 相關 API 重新命名以統一語意：
  - `ProgressNode.SetComplete` → `SetMark`、`IsCompletedSelf` → `IsMarked`、`CompleteTime` → `MarkedTime`（舊名稱保留為 `[Obsolete]` 別名）
  - `Sheet.SetCompleteIncludeNonNode` / `SetUnmarkIncludeNonNode` → `SetProgressMark` / `SetProgressUnmark`（**無** Obsolete 別名，需自行改名）
  - `Sheet.ProgressCompletionDic` → `AllMarkedProgressDic`（`ProgressCompletions` / `ProgressCompletionsLocalDateTime` 仍保留為 `[Obsolete]` 別名）
  - 上述所有舊名稱的相容別名統一移至獨立的 `Runtime/Obsolete/` 檔案維護
- **(Breaking)** `LearningPortfolio.CreateUserProjectRecordShower` 改名為 `CreateUserProjectSheetShower`，並新增未連線（`IsConnected == false`）時直接拋出例外的防呆（舊名稱保留為 `[Obsolete]` 別名）
- **(Breaking)** `sheet.IsAnyNetSerivceRequesting`（原拼字錯誤）改名為 `IsAnyNetServiceRequesting`，**無**相容別名
- **(Breaking)** 非同步（`RequestAsync`）呼叫風格的結果狀態從三態（`Success` / `Failed` / `Exception`）簡化為兩態（`Success` / `Failed`）：`LearningPortfolioApiException`（可預期的後端/業務錯誤）繼續回傳 `.IsFailed`、不拋出；但**其餘未預期的例外與請求取消（`OperationCanceledException`）現在會直接從 `RequestAsync()` 拋出**，不再靜默包裝進結果內（`.IsException` / `.Exception` 欄位已移除）。Callback 風格的 `.Request(...)` 行為不變，仍然透過 `onFailure` / `onException` 回報、不會拋出
- 移除 BasicAssets 範例內建的 DeepLink 登入流程（`EWovaLoginPlane.DeepLink.cs`）與 asmdef 對應參照
- 修正多處拼字錯誤：`bloackedMsg` → `blockedMsg`（`ConnectBlocker` 的 tuple 欄位）、UI 內部 `hoving` → `hovering`、`DisableOriginCloseButtonBehaviour` → `DisableOriginCloseButtonBehavior`
- Editor 內「你正在使用測試 / 正式環境 API URL」提醒，改為在使用測試環境時顯示（原本方向相反，正式環境時才顯示）
### Fixed
- 修正 `ClearReadableData` 清除頁面資料後，該頁與總覽頁的欄位總結不會重新計算、殘留舊值的問題
- 修正頁面沒有欄位、或後台回傳空的欄位總結時，欄位總結未被清空、殘留錯誤舊值的問題；並放寬總覽頁「欄位數需 ≥ 2」的判斷，避免只有單一總結欄位的總覽頁被誤判為「欄位不足」而略過總結計算
- 修正取消網路請求（`CancelAll` / `Dispose`）時，佇列中尚未執行的請求可能導致呼叫端的 `await` 永遠停留在 pending 狀態、收不到取消結果的問題
- 移除 BasicAssets 範例內建的 `LearningPortfolioProfile.asset`，避免使用者透過 Package Manager 重新匯入範例時，不小心覆寫並遺失自己專案已設定好的 APIKey
## [2026.8.3] - 2026-07-31
### Added
- 新增 `LearningPortfolio.ConnectBlocker`，讓開發者可以自訂連線前置檢查邏輯（例如遊戲進行中不允許連線），檢查未通過時 `ConnectStatus` 會回傳新增的 `ConnectBlockedByCustomLogic` 狀態
- 新增 `LearningPortfolio.ConnectedProject`，可取得目前已連線的專案資訊
- Unity Editor 匯入套件後新增歡迎視窗（Welcome Window），提供版本資訊與快速入門連結
- 使用 DeepLink 登入時，等待逾 8 秒會顯示「PC Link」連線提示（XR 環境）；等待超過 60 秒則自動取消登入流程並顯示逾時訊息
- 範例新增 `ConnectBlocker` 阻擋連線的客戶端自訂邏輯範例，範例程式碼統一移入 `EWova.LearningPortfolio.BasicAssets` namespace 下
- `ConnectResponse` / `CheckAvailabilityResponse` 等連線結果新增 `ClientErrorMessage` 欄位，提供更友善、可直接顯示給使用者的本地端錯誤說明
### Changed
- 驗證流程統一改由 `LearningPortfolio.EWovaAuth` 執行個體管理，取代原先透過 Core 套件內單例呼叫的方式，降低套件間耦合
- **(Breaking)** `ApiSettings` 結構重新命名為 `ProjectSettings`；`LearningPortfolioProfile.APISettings` 欄位改名為 `ProjectSettings`（已透過 `FormerlySerializedAs` 保留舊資料，Inspector 現有設定不會遺失，但直接以程式碼參照 `ApiSettings` / `APISettings` 的專案需自行更新）
- `LPApiClient.GetTex2D` 新增 `IProgress<float>` 參數，可回報圖片下載進度
### Fixed
- 移除範例中已棄用的 `LearningPortfolioEditorSettings.SkipForceLoginForBrowserAuthorization` 用法
- 修正 IL2CPP 在較高 Managed Stripping Level 下，可能將 API 回應用的 Model 類別建構子/欄位視為未使用而裁剪，導致打包後於裝置上反序列化失敗或欄位遺失的問題
- 修正例外訊息遺失的問題，`LearningPortfolioApiException.Message` 現在會正確回傳原始 API 例外訊息
- 連線與取得學習歷程失敗時，現在會正確填入 `Exception` / `ClientErrorMessage` / `ServerErrorMessage`，不再遺失錯誤追蹤資訊
- `ConnectAsync` 加入重入保護，避免併發呼叫重複建立 client / GameObject / 心跳迴圈，造成資源洩漏
- 修正例外處理中 catch 類型不匹配的問題，導致 `onException` 無法被正確觸發
- 修正 `CheckAvailabilityAsync` 成功時未釋放暫時用的 client，造成每次登入流程都有資源洩漏
- 心跳迴圈於 Dispose 時發生例外現在會記錄 log，不再被靜默吞掉
- 修正取得使用者紀錄清單為空陣列、或首頁欄位數不足時直接丟出 `IndexOutOfRangeException` 的問題，改為丟出明確例外或記錄警告後略過
- 使用者紀錄圖示改為逐張下載並各自 try/catch，單張圖片下載失敗不再中斷整個學習歷程載入流程
- 修正 DeepLink 跳轉授權未統一透過 `LearningPortfolio.EWovaAuth` 執行個體處理的問題
- 修正併發呼叫 `ConnectAsync` 時，等待中的呼叫可能誤判 `Instance` 是否為 null 而回傳錯誤結果，現在會正確鏡射實際執行連線那次呼叫的完整結果（狀態、例外、錯誤訊息）
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