# PiKoRe — Current State

*Update this file at the end of every development session. It is the first thing to read at the start of a new session.*

---

## Phase
**Phase 3 complete. Pre-Phase-4 interface and doc cleanup complete. Ready to start Phase 4.**

---

## Last session
Post-Phase-3 review identified and resolved several design gaps before Phase 4 implementation begins. All changes are backward-compatible with Phase 3 code (no existing implementations affected — these are interface and doc changes only).

Changes applied:
- `IMediaStore` refactored (D-027): `GetByIdAsync` removed, replaced with `GetFileDetailsAsync → FileAnalysisDetails?`; `SearchByEmbeddingAsync` now returns `IReadOnlyList<Guid>` instead of `IReadOnlyList<IndexedFile>`
- New `FileAnalysisDetails` record in `Core/Models/` — aggregates PostgreSQL analysis data for one file
- New `IFileIndexStore` interface in `Core/Abstractions/` — owns all SQLite `file_index` operations (D-028); `SqliteFileIndexStore` implementation deferred to Phase 5
- `IMediaStore.UpsertThumbnailAsync` added (was missing; required by Phase 6 ThumbnailPlugin)
- `docs/architecture.md` updated: EF Core removed from tech inventory, net10 target, `supported_media_types` in manifest example, D-021 dispatch model clarified
- `.github/copilot-instructions.md` target updated to .NET 10
- `ACTION_PLAN.md` expanded: Phase 4 full DI wiring list, Phase 5 pre-req (IFileIndexStore), Phase 9 two-store search flow
- D-027 and D-028 appended to DECISIONS.md

`dotnet build`: 0 errors, 2 warnings (Avalonia transitive dep — not actionable).

---

## What was done (Phase 3 + pre-Phase-4 cleanup)

### Phase 3
- `Models/Job.cs` — `FilePath?` and `MediaType?` (transient; dequeue-populated via JOIN)
- `Models/JobResult.cs` — `FileId`, `Capability`, `MediaType?` (D-026)
- `Abstractions/IJobQueue.cs` — 3 new methods for DagEngine
- `Data/SqliteJobQueue.cs` — full `IJobQueue` implementation (Dapper, `BEGIN IMMEDIATE`)
- `Core/Pipeline/LocalSequentialRunner.cs` — `IJobRunner` (semaphores, Polly retry, media-type check)
- `Core/Pipeline/PipelineWorker.cs` — 100ms-poll `BackgroundService`
- `Core/Pipeline/DagEngine.cs` — `INotificationHandler<JobCompletedEvent>` (ordering guard included)
- `Core.Tests/Pipeline/LocalSequentialRunnerTests.cs` — 2 tests, 2/2 passing

### Pre-Phase-4 cleanup
- `Models/FileAnalysisDetails.cs` — new record (FileId, Metadata, Tags, ThumbnailPath, HasEmbedding, Description)
- `Abstractions/IMediaStore.cs` — `GetFileDetailsAsync`, `SearchByEmbeddingAsync → IReadOnlyList<Guid>`, `UpsertThumbnailAsync`
- `Abstractions/IFileIndexStore.cs` — `GetByPathAsync`, `GetByIdAsync`, `UpsertAsync`

---

## Next concrete step

**Phase 4 — Plugin Host + Debug Endpoint**

Create `src/PiKoRe.Host/` — new console app project. Full DI wiring task list is in ACTION_PLAN.md Phase 4.

Key things the host must wire:
```
SqliteJobQueue          → IJobQueue          (singleton)
SqlitePluginRegistry    → IPluginRegistry    (singleton)
LocalSequentialRunner   → IJobRunner         (singleton)
ExifPlugin, ThumbnailPlugin → IInProcessPlugin (singleton each)
DagEngine               → registered via MediatR assembly scan (covers Core assembly)
PipelineWorker          → AddHostedService<PipelineWorker>()
Kestrel                 → Listen(:7700) + Listen(:7701)
Route groups            → RequireHost("*:7700") / RequireHost("*:7701")
```

DB migrations run on startup before `app.Run()`.

Done when: `curl :7700/api/plugins/register` adds a plugin; `curl :7701/debug/plugins` returns it.

---

## Open questions / blockers
- `Tmds.DBus.Protocol` 0.16.0 vulnerability warning (NU1903): transitive through Avalonia. Not actionable.
- `SqliteFileIndexStore` not yet implemented — needed by Phase 5 (FileScanner). Interface is defined; implementation is Phase 5 pre-req.

---

## Recent decisions
- D-026: `JobResult` carries `MediaType` — avoids extra DB round-trip in `DagEngine`
- D-027: `IMediaStore` is PostgreSQL-only; `GetByIdAsync` replaced with `GetFileDetailsAsync`; `SearchByEmbeddingAsync` returns file IDs
- D-028: `IFileIndexStore` owns all SQLite `file_index` operations; `FileScanner` depends on it

---

## Known issues / tech debt
- NU1903 warning from `Tmds.DBus.Protocol` 0.16.0 — no action until Avalonia updates
- `DequeueAsync` orphans a job if its `file_index` row is deleted between enqueue and dequeue — acceptable for MVP
- `SqliteFileIndexStore` not yet created — Phase 5 pre-req
- No `PostgresMediaStore` implementation yet — needed by Phase 6 (plugins write analysis results)
