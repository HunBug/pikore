# PiKoRe — Current State

*Update this file at the end of every development session. It is the first thing to read at the start of a new session.*

---

## Phase
**Phase 1 complete. Starting Phase 2.**

---

## Last session
Phase 1 — .NET Solution Skeleton. Created all projects, wired references, added NuGet packages. `dotnet build` exits 0.

---

## What was done (Phase 1)

- `.gitignore` already in place (renamed from `gitignore` in a prior commit)
- `PiKoRe.slnx` created at repo root (.NET 10 SDK uses the new `.slnx` XML format instead of classic `.sln`)
- All 7 projects created, all targeting `net10.0`:
  - `src/PiKoRe.Core/` — classlib
  - `src/PiKoRe.Data/` — classlib
  - `src/PiKoRe.UI/` — Avalonia app (`avalonia.app` template, TFM corrected to `net10.0`)
  - `src/PiKoRe.Plugins.Exif/` — classlib
  - `src/PiKoRe.Plugins.Thumbnails/` — classlib
  - `tests/PiKoRe.Core.Tests/` — xunit
  - `tests/PiKoRe.Data.Tests/` — xunit
- All project references wired (see ACTION_PLAN.md Phase 1 for the graph)
- NuGet packages added per project (see individual `.csproj` files)
- Auto-generated `Class1.cs` / `UnitTest1.cs` stubs deleted
- `src/PiKoRe.Data/Migrations/SQLite/`, `src/PiKoRe.Data/Migrations/PostgreSQL/`, and `plugins/` created with `.gitkeep`
- `dotnet build`: **0 errors, 2 warnings** (both NU1903 from transitive Avalonia dep `Tmds.DBus.Protocol` 0.16.0 — unfixable until Avalonia ships a newer version)
- `Polly.Extensions.Http` was skipped — deprecated in Polly v8; base `Polly` package (8.6.6) covers all needs. `Microsoft.Extensions.Http.Polly` should be added to `PiKoRe.Host` in Phase 4 when HTTP resilience is needed.

---

## Next concrete step

**Phase 2a — Core interfaces** in `src/PiKoRe.Core/`:

Create the following files (see ACTION_PLAN.md Phase 2 for exact signatures):
```
src/PiKoRe.Core/
  Abstractions/
    IPlugin.cs
    IInProcessPlugin.cs
    IExternalPlugin.cs
    IPluginRegistry.cs
    IJobQueue.cs
    IJobRunner.cs
    IFileScanner.cs
    IMediaStore.cs
  Models/
    Job.cs
    JobResult.cs
    AnalysisRequest.cs
    AnalysisResult.cs
    IndexedFile.cs
    JobStatus.cs
  Constants/
    Capabilities.cs
  Events/
    FileIndexedEvent.cs
    JobCompletedEvent.cs
    JobFailedEvent.cs
    PluginRegisteredEvent.cs
```

Then Phase 2b/2c: write the SQL migration files. Then Phase 2d: `DatabaseMigrator.cs`.

---

## Open questions / blockers
- `Tmds.DBus.Protocol` 0.16.0 vulnerability warning (NU1903): transitive through `Avalonia 11.1.0`. Not actionable until Avalonia releases a patched version. Logged in DISCOVERIES.md.

---

## Recent decisions
- D-017: Used `net10.0` (SDK 10.0.104 LTS) instead of `net9.0` (user preference for LTS track)
- D-018: `Polly.Extensions.Http` omitted — deprecated in Polly v8; revisit with `Microsoft.Extensions.Http.Polly` in Phase 4

---

## Known issues / tech debt
- NU1903 warning from `Tmds.DBus.Protocol` 0.16.0 (Avalonia transitive dep) — no action possible until Avalonia updates its dependency
