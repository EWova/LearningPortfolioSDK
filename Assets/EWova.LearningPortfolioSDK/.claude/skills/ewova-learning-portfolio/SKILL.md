---
name: ewova-learning-portfolio
description: Integrating the EWova LearningPortfolioSDK Unity package — the static `EWova.LearningPortfolio.LearningPortfolio` API, `EWovaLoginPlane` login UI prefab, `LearningPortfolioProfile` asset, `UserProjectRecordSheet`/`ProgressNode`/`Page`/`Row`/`Column` data model, and `ProjectRecordShower` UI. Trigger whenever the user mentions LearningPortfolioSDK, EWova login/connect flow, or these class names.
---

# EWova LearningPortfolioSDK

Unity UPM package for connecting a game/app to EWova's "Learning Portfolio" backend: user login,
per-user progress tracking (a tree of completable nodes), and a spreadsheet-like per-user record
(pages/rows/columns/cells) that the game reads and writes.

## Install

Add to `Packages/manifest.json` (or via Package Manager → Add package from git URL). The SDK depends
on `com.ewova.core`, which is **not** auto-resolved by git-URL dependencies in UPM, so add both:

```json
"dependencies": {
  "com.ewova.core": "https://github.com/EWova/UnityPackageCore.git?path=Assets/EWova.Core",
  "com.ewova.learningportfoliosdk": "https://github.com/EWova/LearningPortfolioSDK.git?path=Assets/EWova.LearningPortfolioSDK"
}
```

The repo's release tags (e.g. `v1.3.0`) are older than the current `package.json` version — for a
reproducible build, pin a specific commit hash (`...git?path=...#<commit-sha>`) rather than tracking
`dev`/`master` HEAD.

After install, import the **BasicAssets** sample from Package Manager → this package → Samples. It
contains a fully worked reference project (`Template.cs`, `YourGame.cs`, `PlayerDataUploader.cs`,
`ConnectBlocker.cs`) that this skill's examples are drawn from — look there for a runnable demo.

## Setup (required before any API call)

1. Create a config asset: **Assets → Create → EWova → LearningPortfolio → Profile**, producing a
   `LearningPortfolioProfile` ScriptableObject.
2. Set its **API Key** in the inspector (a custom editor lets you click "驗證" to test the key live).
3. **The asset must live at exactly `Resources/EWova/LearningPortfolioProfile.asset`** (any `Resources`
   folder in the project) — `LearningPortfolio.LoadProjectSettings()` calls
   `Resources.Load<LearningPortfolioProfile>("EWova/LearningPortfolioProfile")` with that hardcoded path.
   Wrong path/folder = silent `IsProjectSettingsValid == false` at runtime, not a compile error.
4. Drop the `EWovaLoginPlane` prefab (`Resources/EWova/LearningPortfolio/EWovaLoginPlane.prefab`) into
   your first scene and wire its `OnGameStart(bool isLogin)` UnityEvent to your game-start logic. This
   prefab already contains an `EWovaLoginPlaneUI` + on-screen keyboard child — you normally don't touch
   those sub-components directly.

## Concept map

| Type | Role |
|---|---|
| `EWova.LearningPortfolio.LearningPortfolio` | Static entry point: connect/disconnect, current user, current record sheet, events. Also a `MonoBehaviour` internally, but you never instantiate it yourself. |
| `EWovaLoginPlane` (+ `EWovaLoginPlaneUI`) | Drop-in login UI prefab that drives `LearningPortfolio.CheckAvailability`/`Connect`/`Disconnect` for you and fires `OnGameStart(bool)`. |
| `LearningPortfolioProfile` | ScriptableObject holding the project's `ProjectSettings.APIKey`. Must sit under a `Resources/EWova/` folder. |
| `LearningPortfolio.UserProjectRecordSheet` | The logged-in user's whole record: `ProgressNode` tree + `Page[]`. Obtained via `LearningPortfolio.LoggedUserProjectRecordSheet` or the `FetchUserProjectSheet`/`OnUserProjectRecordUpdated` callback. |
| `LearningPortfolio.ProgressNode` | One node in a completion tree (e.g. `"單元1/關卡1"`). Has `SetMark`/`SetUnmark` write handles. |
| `LearningPortfolio.Page` / `Column` / `Row` / `Cell` | Spreadsheet-like data per page: fixed `Column`s, 1-indexed `Row`s, each `Row` has `SetCells`; `Page` has `AddRow`/`AddRowAndSetCells`/`ClearReadableData`. |
| `NetServiceVoid` / `NetServiceRequest<T>` / `NetServiceRespond<T>` / `NetService<TRequest,TRespond>` | The write-handle types exposed as properties on the model above (`.Request(...)` callback style or `.RequestAsync(...)` awaitable style). All network writes for one sheet are serialized through one internal queue — you never race two writes on the same user. |
| `ProjectRecordShower` | Read-only viewer UI (progress graph + spreadsheet) for the current user's record. Created via `LearningPortfolio.CreateUserProjectSheetShower(rectTransform)`, not instantiated directly. |
| `Api.SetRowRequest` / `Api.AddRowResponse` | The `Api.*` (raw backend DTO) types you construct yourself, as payloads to the `NetService*` calls above. Every other `Api.*` type (`Api.Project`, `Api.Sheet`, `Api.ProgressNode`, ...) is an internal wire-format DTO the SDK deserializes into the friendlier types above — don't construct or depend on them, except `Api.Project` which is directly exposed read-only as `LearningPortfolio.ConnectedProject`. `Api.SetColumnRequest` / `Column.Edit` are `[Obsolete]` — column field type is now backend-managed; `Edit` logs a warning and no-ops instead of sending. |
| `LearningPortfolioApiException` and subclasses (`ApiSheetException`, `ApiUsageException`, `ApiProjectException`, `ApiLeaderboardException`) | Thrown from awaited `*Async` calls on backend failure; carry `.Action`, `.SourceApiEx`. The `.Request(...)` callback style instead routes failures to `onFailure`/`onException` — it never throws. |

**Not for third-party use** (looks reachable but isn't):
- `LPApiClient` — its constructors are `internal`/`internal protected`. It's created and owned by
  `LearningPortfolio` internally; you never `new` one.
- `LearningPortfolioEWovaAuth` (the type of `LearningPortfolio.EWovaAuth`) — `internal` constructor and
  `internal` `ApiKey` setter. Read it (e.g. `LearningPortfolio.EWovaAuth.IsAuthenticated`,
  `.CurrentUser`) but don't construct one; those members actually come from `EWova.Auth.AuthProvider`
  in the `com.ewova.core` dependency, not this package.
- `EWova.Authoring.*` (`LearningPortfolioEditorPrefs`, `EditorDomainReleaseHelper`, `DevelopTip`,
  `EditorLogger`) and everything under `Editor/` — compiled only in `UNITY_EDITOR`, drive the Welcome
  Window / custom inspector / dev-only "disable force login" toggle. Not runtime API.
- `EWova.LearningPortfolio.PackageInfo` — `internal`, auto-generated version constants.
- `EWova.VirtualKeyboard.*` — internal implementation detail of the on-screen keyboard inside
  `EWovaLoginPlaneUI`; only relevant if you're rebuilding that prefab's input handling yourself.
- Bare `Api.*` response DTOs other than `Api.Project`/`Api.SetRowRequest`/`Api.AddRowResponse`
  — marked `[Preserve]` purely so IL2CPP stripping doesn't break JSON deserialization; not meant to be
  constructed by callers.

## Usage flow

### 1. Login via the prefab (recommended)

```csharp
// Template.cs (from the BasicAssets sample)
public EWovaLoginPlane loginPlane;

void OnEnable()
{
    loginPlane.OnGameStart.AddListener(OnStart);
    LearningPortfolio.OnUserLogin += HandleUserLogin;
    LearningPortfolio.OnUserLogout += HandleUserLogout;
    LearningPortfolio.OnUserProjectRecordUpdated += HandleUserProjectRecordUpdated;
}

void OnStart(bool isLogin) // fired by the login prefab once the user picks "start"/"skip"
{
    if (isLogin)
        Debug.Log($"登入開始 使用者身分:{LearningPortfolio.LoginUserData}");
    UnityEngine.SceneManagement.SceneManager.LoadScene("YourGame");
}
```

The prefab internally drives `LearningPortfolio.CheckAvailability` → user clicks login →
`LearningPortfolio.Connect` (opens a system-browser deep-link OAuth flow) → fetches the record sheet —
you don't need to call those yourself if you use the prefab.

### 2. Programmatic connect (no prefab)

```csharp
var process = new ConnectProcess();
process.OnCompleted += result =>
{
    if (result.IsSuccess) { /* LearningPortfolio.IsConnected is now true */ }
    else if (result.IsManuallyCancel) { /* user cancelled */ }
    else { Debug.LogError($"{result.Status} {result.ClientErrorMessage} {result.ServerErrorMessage}"); }
};
LearningPortfolio.Connect(process, cancellationToken: someCts.Token);
// or: await LearningPortfolio.ConnectAsync(process, ct);
```

`CheckAvailabilityProcess`/`FetchProjectSheetProcess` follow the same `Process<T,TStatus>` pattern
(`OnProgressChanged`, `OnStatusChanged`, `OnCompleted`, `IsSuccess`, `IsManuallyCancel`).

### 3. Reading the current user's record

```csharp
if (!LearningPortfolio.IsConnected) return;
var sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

float progress = sheet.CompletionProgress;              // 0.0–1.0
string[] pageLabels = sheet.GetPagesLabel();
sheet.FindProgressNodeByPath("單元1/關卡1", out var node);
bool done = node.IsCompleted;                            // self, child, or parent completion
```

### 4. Writing progress / cells — see `references/data-model.md` for the full read/write API,
row-indexing gotcha, and more worked examples (progress nodes, page cells, `ConnectBlocker` pattern).

### 5. Showing the built-in record viewer UI

```csharp
// Throws if called before IsConnected is true.
ProjectRecordShower shower = LearningPortfolio.CreateUserProjectSheetShower((RectTransform)someParent);
// later:
shower.Close();
```

## Common pitfalls

- **`Resources/EWova/LearningPortfolioProfile.asset` path is exact and case-sensitive.** Any other
  location fails `LoadProjectSettings` silently at runtime (no compile-time check).
- **Row indices are 1-based**, not 0-based, by deliberate API design (`targetPage.Rows[1]` is the
  first row). Column indices are 0-based.
- **Always guard on `LearningPortfolio.IsConnected`** before touching `LoggedUserProjectRecordSheet` —
  it's `null` when not connected, and all samples check this first.
- **Also guard writes on `LearningPortfolio.IsUpdatingUserProjectRecord`** — `true` while a previous
  write for the current user's sheet is still in flight. Requests still queue safely if you don't, but
  checking it lets you disable a submit button / skip a redundant write instead of silently piling up.
- **`IsLoggedIn` is `[Obsolete]`** — use `IsConnected` instead (identical value now, but `Connect` used
  to only mean "authenticated", now it means "authenticated + record sheet loaded").
- **Never construct `LPApiClient` or `LearningPortfolioEWovaAuth` yourself** — both have internal-only
  constructors; go through the static `LearningPortfolio` methods.
- **Writes are per-sheet serialized**, not per-call: rapid-fire `.Request(...)` calls on the same sheet
  queue up rather than race, but a write to sheet A and a write to sheet B (different users) are
  independent queues.
- Use `LearningPortfolio.ConnectBlocker` (a `List<Func<(bool isBlocked, string blockedMsg)>>`) to prevent
  reconnect while some other state (e.g. a learning session already in progress) makes it unsafe — see
  the `ConnectBlocker.cs` sample for the idiomatic self-registering pattern.
- `LearningPortfolio.ChartCellViewRenderer` (`Func<(bool isReadonly, FieldType fieldType, string text),
  LearningPortfolio.ChartCellDisplay>`) controls how `ProjectRecordShower`'s built-in chart renders each
  cell. It only receives the column's `IsReadOnly`/`FieldType` and the cell's raw text (not the full
  `Column`/`Cell` objects), and returns a `ChartCellDisplay { LabelText, OverrideAlignment }` —
  deliberately narrow so third parties can't reach into internal state. Reassign it to fully replace the
  view logic, or wrap `LearningPortfolio.DefaultChartCellViewRenderer` to tweak its output (the default
  renderer already picks different colors for read-only vs. editable cells). This is a single overridable
  delegate, not an additive list like `ConnectBlocker` — the last assignment wins.
- The login prefab has an editor-only "Disable Force Login" convenience toggle
  (`EWova/Editor/Learning Portfolio/Disable Force Login`) that only affects Play Mode in the Editor,
  never a build.

## Detailed reference

See `references/data-model.md` for: the full `ProgressNode`/`Page`/`Column`/`Row`/`Cell` write API
(`NetServiceVoid`/`NetServiceRequest<T>`/`NetServiceRespond<T>`/`NetService<TRequest,TRespond>`,
callback vs. `Async` styles), `SheetHelper`/`[Column]` object↔row mapping, a recommended
Scheme+Manager project architecture for organizing page/row types and progress-node paths, and
additional worked examples lifted directly from the BasicAssets sample (`PlayerDataUploader.cs`).
