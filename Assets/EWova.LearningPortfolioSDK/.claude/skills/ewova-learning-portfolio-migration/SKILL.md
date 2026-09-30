---
name: ewova-learning-portfolio-migration
description: Migrating a project's code off the renamed/breaking public API from EWova LearningPortfolioSDK 1.3.0 up to 2026.8.4 — renamed Sheet/Progress members, the API→Api DTO namespace casing change, and LearningPortfolio-class renames. Trigger when the user says they're upgrading from an old/1.3.0 LearningPortfolioSDK project, or when compile errors point at a name in the table below (e.g. WriteToRow, SetCompleteIncludeNonNode, IsCompletedSelf, API.SetRowRequest, CreateUserProjectRecordShower, IsLoggedIn, LearningPortfolioProfile.APISettings).
---

# LearningPortfolioSDK migration: 1.3.0 → 2026.8.4

A project still on 1.3.0 (or earlier) will hit compile errors on the renamed members below after
upgrading the package. This is a pure find-and-replace lookup table — apply it directly to the user's
code rather than guessing at fixes from the error text alone.

## Sheet / Progress API

| Old (1.3.0) | New (2026.8.4) | Obsolete alias? |
|---|---|---|
| `API.SetRowRequest` / `API.AddRowResponse` / `API.SetColumnRequest` / `API.Project` | `Api.SetRowRequest` / `Api.AddRowResponse` / `Api.SetColumnRequest` / `Api.Project` | ❌ |
| `SheetHelper.WriteToRow(object, Row)` | `SheetHelper.AlignToColumns(object, Page)` | ❌ (parameter type also changed Row → Page) |
| `sheet.IsAnyNetSerivceRequesting` (typo) | `sheet.IsAnyNetServiceRequesting` | ❌ |
| `Sheet.SetCompleteIncludeNonNode` | `Sheet.SetProgressMark` | ❌ |
| `Sheet.SetUnmarkIncludeNonNode` | `Sheet.SetProgressUnmark` | ❌ |
| `ProgressNode.SetComplete` | `ProgressNode.SetMark` | ✅ |
| `ProgressNode.IsCompletedSelf` | `ProgressNode.IsMarked` | ✅ |
| `ProgressNode.CompleteTime` | `ProgressNode.MarkedTime` | ✅ |
| `Sheet.ProgressCompletions` / `ProgressCompletionsLocalDateTime` | `Sheet.AllMarkedProgressDic` | ✅ |
| `NetSerivceVoid` / `NetSerivceRequest<T>` / `NetSerivceRespond<T>` (typo) | `NetServiceVoid` / `NetServiceRequest<T>` / `NetServiceRespond<T>` | ❌ |

## `LearningPortfolio` class

| Old (1.3.0) | New (2026.8.4) | Obsolete alias? |
|---|---|---|
| `LearningPortfolio.CreateUserProjectRecordShower` | `CreateUserProjectSheetShower` | ✅ |
| `LearningPortfolio.IsLoggedIn` | `IsConnected` | ✅ |
| `LearningPortfolioProfile.APISettings` (field/type) | `LearningPortfolioProfile.ProjectSettings` (field/type) | ⚠️ Inspector data preserved via `FormerlySerializedAs`; no code-level alias |

## Out of scope for this table

The login/connect flow (`Login`/`Connect(APISettings,...)`) was fully redesigned into
`ConnectProcess`/`CheckAvailabilityProcess` + DeepLink OAuth between 1.3.0 and 2026.8.4 — that's not a
rename, it needs re-integrating against the current usage flow (see the `ewova-learning-portfolio`
skill) rather than a find-and-replace.

Full version-by-version history: `Assets/EWova.LearningPortfolioSDK/changelog.md`.
