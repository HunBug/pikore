# PiKoRe — Current State

*Update this file at the end of every development session. It is the first thing to read at the start of a new session.*

---

## Phase
**Phase 2 complete. Starting Phase 3.**

---

## Last session
Pre-Phase 3 architectural decisions (D-021, D-022): `IExternalPlugin` removed, `ExternalPluginInfo` record added, `IPluginRegistry` updated. These are breaking changes to Phase 2 interfaces — made before any Phase 3 implementation started, so no concrete code was affected. `dotnet build`: 0 errors, 2 warnings (same Avalonia transitive dep).

---

## What was done (Phase 2 + pre-Phase 3 cleanup)

### 2a — Core interfaces (`src/PiKoRe.Core/`)
- `Models/` — `JobStatus` (enum), `IndexedFile`, `AnalysisRequest`, `AnalysisResult` (+ `FaceResult`), `Job`, `JobResult`, `ExternalPluginInfo`
- `Constants/Capabilities.cs` — `Exif`, `Thumbnail`, `Embedding`, `Tags`, `Faces`, `Description`, `NsfwScore`, `AestheticScore`
- `Abstractions/` — `IPlugin`, `IInProcessPlugin`, `IPluginRegistry` (uses `ExternalPluginInfo`), `IJobQueue`, `IJobRunner`, `IFileScanner`, `IMediaStore`
- `Events/` — `FileIndexedEvent`, `JobCompletedEvent`, `JobFailedEvent`, `PluginRegisteredEvent` (all `INotification` records)
- `IExternalPlugin` was created in Phase 2 then **removed** (D-022) — replaced by `ExternalPluginInfo` record

### 2b — SQLite schema
- `src/PiKoRe.Data/Migrations/SQLite/0001_initial_schema.sql` — `plugin_registry`, `file_index`, `job_queue`, `pipeline_config`, `system_config`. WAL mode enabled.

### 2c — PostgreSQL schema
- `src/PiKoRe.Data/Migrations/PostgreSQL/0001_initial_schema.sql` — `thumbnails`, `metadata`, `tags`, `embeddings`, `faces`, `persons`, `face_person`, `scores`, `descriptions`. pgvector extension enabled. HNSW index on `embeddings.vector`.

### 2d — DbUp wiring + test
- `src/PiKoRe.Data/DatabaseMigrator.cs` — static class, embedded-resource SQL scripts, `MicrosoftLogAdapter` bridges `ILogger` to `IUpgradeLog`.
- SQL files embedded via `<EmbeddedResource Include="Migrations/**/*.sql" />` in `PiKoRe.Data.csproj`.
- `tests/PiKoRe.Data.Tests/DatabaseMigratorTests.cs` — 2 tests: schema applied, idempotency. Uses `pgvector/pgvector:pg16` image via Testcontainers.

---

## Next concrete step

**Phase 3 — Job Queue + Pipeline Engine** in `src/PiKoRe.Data/` and `src/PiKoRe.Core/`:

```
src/PiKoRe.Data/
  SqliteJobQueue.cs          — implements IJobQueue using Microsoft.Data.Sqlite
src/PiKoRe.Core/
  Pipeline/
    LocalSequentialRunner.cs — implements IJobRunner (1 GPU slot, N CPU slots, Polly retry)
    PipelineWorker.cs        — BackgroundService, polls IJobQueue, dispatches to IJobRunner
    DagEngine.cs             — INotificationHandler<JobCompletedEvent>, enqueues unblocked jobs
tests/PiKoRe.Core.Tests/
  Pipeline/
    LocalSequentialRunnerTests.cs — NSubstitute stubs, asserts JobCompletedEvent published
```

Done when: `PiKoRe.Core.Tests` verifies enqueue → run → `JobCompletedEvent` with a stub plugin.

---

## Open questions / blockers
- `Tmds.DBus.Protocol` 0.16.0 vulnerability warning (NU1903): transitive through Avalonia. Not actionable.
- `PiKoRe.Core.Tests` currently has no tests ("No test is available"). That's correct — no logic exists yet. Phase 3 adds the first real test there.

---

## Recent decisions
- D-017: `net10.0` (SDK 10.0.104 LTS)
- D-018: `Polly.Extensions.Http` omitted from Core
- D-019: Solution file is `.slnx`
- D-020: SQL migration files are embedded resources in `PiKoRe.Data.dll`
- D-021: Core dispatch is `IInProcessPlugin`-only. No HTTP in the pipeline engine. External services are wrapped by C# adapter plugins.
- D-022: `IExternalPlugin` interface removed. External plugin metadata is `ExternalPluginInfo` (record). `IPluginRegistry` updated accordingly.

---

## Known issues / tech debt
- NU1903 warning from `Tmds.DBus.Protocol` 0.16.0 — no action until Avalonia updates
