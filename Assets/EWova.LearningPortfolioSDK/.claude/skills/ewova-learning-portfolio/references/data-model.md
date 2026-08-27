# LearningPortfolio data model & write API

All examples below are taken almost verbatim from the package's own `Samples~/BasicAssets`
(`YourGame.cs`, `PlayerDataUploader.cs`, `ConnectBlocker.cs`) — import that sample to run them as-is.

## Shape

```
UserProjectRecordSheet (LearningPortfolio.LoggedUserProjectRecordSheet)
├─ ProgressNode (tree)        e.g. path "單元1/關卡1"
│   ├─ SetComplete : NetSerivceVoid
│   └─ SetUnmark   : NetSerivceVoid
├─ SetCompleteIncludeNonNode : NetSerivceRequest<string>   // mark a path complete even if no node exists for it
├─ SetUnmarkIncludeNonNode   : NetSerivceRequest<string>
└─ Pages[] (fixed columns, 1-indexed rows)
    Page
    ├─ Columns[]              // fixed, 0-indexed
    │   Column.Edit : NetSerivceRequest<Api.SetColumnRequest>
    ├─ Rows[1..N]             // 1-indexed!
    │   Row.SetCells : NetSerivceRequest<Api.SetRowRequest>
    ├─ AddRow : NetSerivceRespond<Api.AddRowResponse>
    ├─ AddRowAndSetCells : NetSerivceRequestRespond<Api.SetRowRequest, Api.AddRowResponse>
    └─ ClearReadableData : NetSerivceVoid
```

## The two calling styles

Every `NetSerivce*` write handle offers both:

- **Callback style** — `.Request(..., onSuccess, onFailure, onException)` — never throws, queued
  fire-and-forget.
- **Awaitable style** — `.RequestAsync(...)` returning `NetServiceAsyncRespond` /
  `NetServiceAsyncRespond<T>` with `.IsSuccess` / `.IsFailed` (+ `.ErrorMessage`) / `.IsException`
  (+ `.Exception`) — also never throws; check the result instead of try/catch.

Both styles go through the same internal per-sheet queue (`NetServiceRequestHandler`), so calls never
race each other for the same user's sheet.

## Progress nodes

```csharp
var sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

// Mark a known node complete
if (sheet.FindProgressNodeByPath("單元1/關卡1", out LearningPortfolio.ProgressNode node1))
{
    node1.SetComplete.Request(
        onSuccess: () => Debug.Log("'單元1/關卡1' 成功標記進度完成"),
        onFailure: (msg) => Debug.LogError("標記進度完成失敗 因為:" + msg),
        onException: (ex) => Debug.LogException(ex)
    );
}

// Awaitable style
UniTask.Void(async () =>
{
    if (sheet.FindProgressNodeByPath("單元1/關卡1", out var node3))
    {
        NetServiceAsyncRespond result = await node3.SetComplete.RequestAsync();
        if (result.IsSuccess) Debug.Log("done");
        else if (result.IsFailed) Debug.LogError(result.ErrorMessage);
        else if (result.IsException) Debug.LogException(result.Exception);
    }
});

// Mark a path complete even when no ProgressNode exists for it (e.g. hidden/legacy progress)
sheet.SetCompleteIncludeNonNode.Request("Extra/額外關卡",
    onSuccess: () => Debug.Log("done"),
    onFailure: (msg) => Debug.LogError(msg),
    onException: (ex) => Debug.LogException(ex));

// Reset all completion
foreach (var path in sheet.ProgressCompletions)
{
    sheet.SetUnmarkIncludeNonNode.Request(path,
        onSuccess: () => Debug.Log($"成功取消進度完成標記 {path}"),
        onFailure: (msg) => Debug.LogError(msg),
        onException: (ex) => Debug.LogException(ex));
}
```

`ProgressNode.IsCompleted` folds together self + all-children-complete + any-parent-complete; use
`IsCompletedSelf` if you need only the node's own flag. `CompleteTime` gives the local completion
timestamp if marked, else `null`.

## Pages / rows / columns (1-indexed rows!)

```csharp
LearningPortfolio.Page targetPage = sheet.Pages[1]; // page index is 0-based (page 0 = overview)

string[] columnLabels = targetPage.GetColumnsLabel();
int rowCount = targetPage.Rows.Count;

// Row indices start at 1, not 0
string[] firstRow = targetPage.Rows[1].GetCellsText();

// Overwrite a row's cells
LearningPortfolio.Row pageTargetRow = targetPage.Rows[1];
string[] cells = pageTargetRow.GetCellsText();
cells[2] = (float.Parse(cells[2]) + 1).ToString(); // e.g. increment a counter column
pageTargetRow.SetCells.Request(
    new Api.SetRowRequest { Cells = cells },
    onSuccess: () => Debug.Log("成功寫入列資料"),
    onFailure: (msg) => Debug.LogError("寫入列資料失敗 因為:" + msg),
    onException: (ex) => Debug.LogException(ex)
);

// Append a new row and set its cells in one call
targetPage.AddRowAndSetCells.Request(
    value: new Api.SetRowRequest { Cells = new[] { "70", "NewV", "66", "101", "123" } },
    onSuccess: (response) => Debug.Log($"成功新增寫入一筆列資料，索引位置為 {response.RowIndex}"),
    onFailure: (msg) => Debug.LogError("新增新列失敗 因為:" + msg),
    onException: (ex) => Debug.LogException(ex)
);

// Clear all readable data on a page (e.g. reset an overview page)
targetPage.ClearReadableData.Request(
    onSuccess: () => Debug.Log("成功清空頁所有資料"),
    onFailure: (msg) => Debug.LogError(msg),
    onException: (ex) => Debug.LogException(ex)
);

// Column metadata (fixed set, doesn't grow)
LearningPortfolio.Column targetColumn = targetPage.Columns[0];
string label = targetColumn.Label;
bool readOnly = targetColumn.IsReadOnly;
LearningPortfolio.FieldType fieldType = targetColumn.FieldType; // Number/String/Boolean — display hint only
string[] allValuesInColumn = targetColumn.GetCellsText();
```

`Api.SetRowRequest.Cells` must line up positionally with the page's columns — the backend fails the
write if a column that has no matching value is expected server-side (see `PlayerDataUploader.cs`'s
`ExampleUpdatePlayCount` comment: "如果後台沒有這一格欄位會存儲失敗").

## Object ↔ row mapping via `SheetHelper` + `[Column]`

For structured data, tag fields with `[EWova.LearningPortfolio.ColumnAttribute]` (optionally with a
custom label) and use `SheetHelper` instead of hand-building `string[]`:

```csharp
public class MyRecord
{
    [Column("總分")] public int Score;
    [Column] public string Name; // uses field name as the column label
}

var record = new MyRecord { Score = 90, Name = "Lucy" };
string[] cells = SheetHelper.WriteToNewRow(record, targetPage); // ready for AddRowAndSetCells.Request

MyRecord readBack = new MyRecord();
SheetHelper.ReadFromRow(someRow, ref readBack);
```

`SheetHelper.TypeFormatters` covers `bool/byte/char/double/int/float/decimal/string/DateTime/TimeSpan`
with round-trippable formatting. `enum` fields are also round-trippable (formatted via `ToString()`,
parsed via `Enum.Parse`); other unregistered types fall back to `Convert.ChangeType`.

Extension-method style is also available via `SheetHelperExtensions`:

```csharp
string[] cells = record.WriteToNewRow(targetPage);
someRow.ReadFromRow(ref readBack);
```

`ReadFrom`/`ReadFromRow` reuse a caller-supplied instance (no allocation). If you'd rather have a new
instance allocated for you, use `CreateFrom`/`CreateFromRow` (requires `T : new()`) — convenient, but
each call allocates a new object, so prefer `ReadFrom`/`ReadFromRow` with a reused instance in hot loops:

```csharp
MyRecord readBack = SheetHelper.CreateFromRow<MyRecord>(someRow);
// or: MyRecord readBack = someRow.CreateFromRow<MyRecord>();
```

`SheetHelper` builds and caches each type's field-mapping reflection data lazily, on first use — not
eagerly at domain load — so types you never touch never get scanned or cached. If you want to avoid the
one-time reflection cost landing on a specific moment (e.g. mid-gameplay), call `SheetHelper.WarmUp<T>()`
or `SheetHelper.WarmUp(typeof(A), typeof(B), ...)` for known types during a loading screen instead:

```csharp
SheetHelper.WarmUp<MyRecord>();
SheetHelper.WarmUp(typeof(OverviewPage), typeof(Level1Page), typeof(Level2Page), typeof(SpPage));
```

The mapping is tiny, so you don't normally need to free it — but if you've warmed up (or otherwise
touched) a large, scene/level-specific set of types you know you're done with, `Release`/`Release(params)`/
`ReleaseAll` evict them from the cache; using the type again afterwards just rebuilds and re-caches it:

```csharp
SheetHelper.Release<MyRecord>();
SheetHelper.Release(typeof(Level1Page), typeof(Level2Page));
SheetHelper.ReleaseAll();
```

## Blocking reconnect while a session is active

```csharp
// ConnectBlocker.cs sample — self-registering guard
[RuntimeInitializeOnLoadMethod]
private static void SetupRuntimeSession()
{
    LearningPortfolio.ConnectBlocker.Add(() =>
        IsLearningSessionActive
            ? (true, "教材已經開始進行，無法登入學習歷程。\n請重新啟動遊戲應用程式")
            : (false, null));
}
public static bool IsLearningSessionActive { get; private set; } = false;
```

This is a **sample file you import and own**, not packaged library code — the setter is `private` in
the stock sample (only the sample's own methods flip it), so add your own setter/method to toggle it
once imported, or edit the accessibility to fit your game's flow.

Any registered `Func<(bool isBlocked, string blockedMsg)>` returning `isBlocked == true` makes
`LearningPortfolio.Connect`/`ConnectAsync` fail fast with `ConnectStatus.ConnectBlockedByCustomLogic`
before any network call.

## Exceptions (awaited-style call sites only)

`LearningPortfolioApiException` (abstract) → `ApiProjectException`, `ApiSheetException`,
`ApiUsageException`, `ApiLeaderboardException`. Each carries `.Action` (an `ApiAction` enum value) and
`.SourceApiEx` (the underlying transport-level `ApiException` from the `com.ewova.core` networking
layer, with `.IsServerError`). These only surface from the `Async` call sites that don't already funnel
errors into `onFailure`/`onException` (e.g. `LearningPortfolio.ConnectAsync`,
`LearningPortfolio.FetchUserProjectSheet`) — the per-field `NetSerivce*.RequestAsync()` calls above
never throw, they return a result struct instead.
