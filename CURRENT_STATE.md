# PiKoRe — Current State

*Update this file at the end of every development session. It is the first thing to read at the start of a new session.*

---

## Phase
**Phase 2 complete. Starting Phase 3.**

---

## Last session
Phase 2 — Core Interfaces + DB Schemas. All public contracts in `PiKoRe.Core` defined, both migration files written, `DatabaseMigrator.cs` wired with DbUp, integration test passes against a live Testcontainers PostgreSQL + pgvector instance. `dotnet build`: 0 errors, 2 warnings (same Avalonia transitive dep as Phase 1). `dotnet test`: 2/2 passed.

---

## What was done (Phase 2)

### 2a — Core interfaces (`src/PiKoRe.Core/`)
- `Models/` — `JobStatus` (enum), `IndexedFile`, `AnalysisRequest`, `AnalysisResult` (+ `FaceResult`), `Job`, `JobResult`
- `Constants/Capabilities.cs` — `Exif`, `Thumbnail`, `Embedding`, `Tags`, `Faces`, `Description`, `NsfwScore`, `AestheticScore`
- `Abstractions/` — `IPlugin`, `IInProcessPlugin`, `IExternalPlugin`, `IPluginRegistry`, `IJobQueue`, `IJobRunner`, `IFileScanner`, `IMediaStore`
- `Events/` — `FileIndexedEvent`, `JobCompletedEvent`, `JobFailedEvent`, `PluginRegisteredEvent` (all `INotification` records)

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
- (Phase 2) SQL migration files are embedded resources in `PiKoRe.Data.dll`, not filesystem paths. More reliable for deployment — no dependency on working directory.

---

## Known issues / tech debt
- NU1903 warning from `Tmds.DBus.Protocol` 0.16.0 — no action until Avalonia updates
