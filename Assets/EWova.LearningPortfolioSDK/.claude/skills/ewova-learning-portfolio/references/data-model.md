# LearningPortfolio data model & write API

All examples below are taken almost verbatim from the package's own `Samples~/BasicAssets`
(`YourGame.cs`, `PlayerDataUploader.cs`, `ConnectBlocker.cs`) — import that sample to run them as-is.

## Shape

```
UserProjectRecordSheet (LearningPortfolio.LoggedUserProjectRecordSheet)
├─ ProgressNode (tree)        e.g. path "單元1/關卡1"
│   ├─ SetMark   : NetServiceVoid
│   └─ SetUnmark : NetServiceVoid
├─ SetProgressMark   : NetServiceRequest<string>   // mark a path complete even if no node exists for it
├─ SetProgressUnmark : NetServiceRequest<string>
└─ Pages[] (fixed columns, 1-indexed rows)
    Page
    ├─ Columns[]              // fixed, 0-indexed
    │   Column.Edit : NetServiceRequest<Api.SetColumnRequest>
    ├─ Rows[1..N]             // 1-indexed!
    │   Row.SetCells : NetServiceRequest<Api.SetRowRequest>
    ├─ AddRow : NetServiceRespond<Api.AddRowResponse>
    ├─ AddRowAndSetCells : NetService<Api.SetRowRequest, Api.AddRowResponse>
    └─ ClearReadableData : NetServiceVoid
```

## The two calling styles

Every `NetService*` write handle offers both:

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
    node1.SetMark.Request(
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
        NetServiceAsyncRespond result = await node3.SetMark.RequestAsync();
        if (result.IsSuccess) Debug.Log("done");
        else if (result.IsFailed) Debug.LogError(result.ErrorMessage);
        else if (result.IsException) Debug.LogException(result.Exception);
    }
});

// Mark a path complete even when no ProgressNode exists for it (e.g. hidden/legacy progress)
sheet.SetProgressMark.Request("Extra/額外關卡",
    onSuccess: () => Debug.Log("done"),
    onFailure: (msg) => Debug.LogError(msg),
    onException: (ex) => Debug.LogException(ex));

// Reset all completion
foreach (var path in sheet.AllMarkedProgressDic.Keys)
{
    sheet.SetProgressUnmark.Request(path,
        onSuccess: () => Debug.Log($"成功取消進度完成標記 {path}"),
        onFailure: (msg) => Debug.LogError(msg),
        onException: (ex) => Debug.LogException(ex));
}
```

`ProgressNode.IsCompleted` folds together self + all-children-complete + any-parent-complete; use
`IsMarked` if you need only the node's own raw flag. `MarkedTime` gives the local completion timestamp
if marked, else `null`.

`sheet.AllMarkedProgressDic` (`IReadOnlyDictionary<string, DateTime>`, path → mark time) is the current
API; `ProgressCompletions`/`ProgressCompletionsLocalDateTime` are `[Obsolete]` aliases kept for backward
compatibility (this property has been renamed twice: `ProgressCompletionDic` →
`ProgressAllCompleteMarkedDic` → `AllMarkedProgressDic`).

**`node.IsMarked` ≠ `node.IsCompleted`** — they answer different questions, and the SDK now gives you a
direct way to check either without touching the raw dictionary yourself:
- `node.IsMarked` (or `sheet.IsProgressNodeMarked(node)` / `sheet.IsProgressMarked(path)`) is the raw
  backend "有沒有被標記完成" flag for exactly that path — it ignores parent/child relationships
  entirely. `AllMarkedProgressDic.ContainsKey(path)` is the same check with no `ProgressNode` required.
- `node.IsCompleted` (or `sheet.IsProgressNodeCompleted(node)` / `sheet.IsProgressCompleted(path)`)
  additionally counts as complete if **any child** is marked, or if **any ancestor** is marked — so a
  parent node can read as complete even though its own path was never directly marked, purely because a
  child underneath it was.

Use `IsMarked`/`IsProgressMarked` when you need "was this exact node explicitly marked" (e.g. deciding
whether to fire `SetMark` again, or a per-step checkmark that shouldn't light up just because a
sibling/child finished). Use `IsCompleted`/`IsProgressCompleted` when you need "should this be
visually/logically treated as done given the whole tree" (e.g. gating whether the player can move on).
`sheet.IsProgress*(path)` internally calls `FindProgressNodeByPath` for you, so prefer them over the
`node.Is*` members when you only have the path string on hand.

## Pages / rows / columns (1-indexed rows!)

Before issuing a write, also guard on `LearningPortfolio.IsUpdatingUserProjectRecord` (in addition to
`IsConnected`) — it's `true` while any write for the current user's sheet is still in flight, so you
can disable a submit button / avoid firing a redundant write instead of just letting requests queue up
silently. Separately, `sheet.IsAnyNetServiceRequesting` is a per-sheet "is a write in flight" flag the
samples poll each frame to drive an "uploading…" UI indicator (see `YourGame.cs`/`PlayerDataUploader.cs`)
— it reflects the write queue itself, not the higher-level reconnect/reload state
`IsUpdatingUserProjectRecord` tracks.

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
    request: new Api.SetRowRequest { Cells = new[] { "70", "NewV", "66", "101", "123" } },
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
string[] cells = SheetHelper.AlignToColumns(record, targetPage); // ready for AddRowAndSetCells.Request

MyRecord readBack = new MyRecord();
SheetHelper.ReadFromRow(someRow, ref readBack);
```

`SheetHelper.TypeFormatters` covers `bool/byte/char/double/int/float/decimal/string/DateTime/TimeSpan`
with round-trippable formatting. `enum` fields are also round-trippable (formatted via `ToString()`,
parsed via `Enum.Parse`); other unregistered types fall back to `Convert.ChangeType`.

Extension-method style is also available via `SheetHelperExtensions`:

```csharp
string[] cells = record.AlignToColumns(targetPage);
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

## Recommended project architecture (Scheme + Manager pattern)

The SDK itself is unopinionated about how you organize page/row types or progress-node paths — this is
a convention worth adopting for any project with more than one or two pages, not something the package
ships or enforces.

**1. A `Scheme` static class — single source of truth for your backend layout.**

```csharp
public static class ProjectScheme
{
    // Semantic progress-node names -> the actual backend path strings.
    // Keeps path strings out of gameplay code and in one place to update if the backend layout changes.
    public enum ProgressNode { 完成教材, 第一關, 第一關_考試測驗, /* ... */ }
    public readonly static IReadOnlyDictionary<ProgressNode, string> ProgressNodeMap = new Dictionary<ProgressNode, string>
    {
        [ProgressNode.完成教材] = "clear",
        [ProgressNode.第一關] = "clear/levell",
        // ...
    };

    // Mirrors the backend's page order (page 0 is conventionally an overview page).
    public enum Level { 第一關 = 1, 第二關 = 2, 特殊測驗 = 3 }

    // One [Column]-tagged class per page, matching that page's fixed columns.
    public class OverviewPageLevelRow
    {
        [Column("總遊玩次數")] public int TotalPlayCount;
        [Column("總遊玩時間")] public TimeSpan TotalPlayTime;
    }

    // A common base exposing which page a row type belongs to lets manager code below stay generic.
    public abstract class LevelRowBase { public abstract Level Level { get; } }

    public class Level1PageRow : LevelRowBase
    {
        public override Level Level => Level.第一關;
        [Column("分數")] public int Score;
        [Column("是否完成關卡")] public bool IsCompletePlay;
    }
}
```

**2. A `Manager` wrapper — one place for the connect/updating guards and typed read/write methods.**

```csharp
public class SheetManager : MonoBehaviour
{
    // ... singleton boilerplate ...

    private void EnsureConnected()
    {
        if (!LearningPortfolio.IsConnected)
            throw new Exception("尚未連接，請先登入");
    }
    private void EnsureSheetNotUpdating()
    {
        if (LearningPortfolio.IsUpdatingUserProjectRecord)
            throw new Exception("仍在上傳中，稍後再試");
    }

    // Generic append works for ANY page's row type, because T.Level says which page to target.
    public void AppendRowData<T>(T writeData, Action<LearningPortfolio.Row> onFinished)
        where T : ProjectScheme.LevelRowBase
    {
        EnsureConnected();
        EnsureSheetNotUpdating();

        var targetPage = LearningPortfolio.LoggedUserProjectRecordSheet.Pages[(int)writeData.Level];
        string[] cells = SheetHelper.AlignToColumns(writeData, targetPage);

        targetPage.AddRowAndSetCells.Request(
            request: new Api.SetRowRequest { Cells = cells },
            onSuccess: response => onFinished?.Invoke(targetPage.Rows[response.RowIndex]),
            onFailure: msg => { onFinished?.Invoke(null); Debug.LogError(msg); },
            onException: ex => { onFinished?.Invoke(null); Debug.LogException(ex); });
    }
}
```

Why this shape:
- **Guards live in one place.** Every write goes through `EnsureConnected`/`EnsureSheetNotUpdating`
  instead of every call site repeating the same two `if`s.
- **`LevelRowBase.Level` drives dispatch.** Because every row type knows its own page, generic methods
  like `AppendRowData<T>`/`SetRowData<T>`/`TryGetRowData<T>` can be written once instead of once per
  page. This only works for pages that share the same interaction shape (append/overwrite one row of N);
  a page that behaves differently — e.g. an overview page with exactly one fixed row per level instead
  of a growing list — gets its own dedicated method pair instead of being forced into the generic one.
- **`ProgressNodeMap` decouples enum names from path strings.** Gameplay code calls something like
  `SetProgressNodeCompleted(ProjectScheme.ProgressNode.第一關)` instead of hardcoding `"clear/levell"`
  at every call site, so the path layout can change in one place later.

Adjust the exact enums/classes to your own backend layout — the pattern (Scheme = data shape, Manager =
guarded access) is what's reusable, not the specific field names above.

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
`LearningPortfolio.FetchUserProjectSheet`) — the per-field `NetService*.RequestAsync()` calls above
never throw, they return a result struct instead.
