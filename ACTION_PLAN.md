# PiKoRe — Action Plan

*Agent-executable task plan. Read CURRENT_STATE.md, DECISIONS.md, and DISCOVERIES.md before starting any phase. Update CURRENT_STATE.md when a phase completes.*

*Format per task: checkbox, description, verifiable done-condition, and any file-level specifics.*

---

## Open Decisions (resolve before Phase 1)

These are pending — record each as a new DECISIONS.md row when confirmed.

| ID | Decision | Recommendation | Status |
|----|----------|----------------|--------|
| D-011 | SQLite file location | Configurable; hardcoded default acceptable for v1 | DECIDED |
| D-012 | Core HTTP port layout | `:7700` plugin registration + job progress; `:7701` debug/admin | DECIDED |
| D-013 | Plugin auto-start in MVP | Core does NOT execute `startup_command` in v1 — plugins pre-started | DECIDED |
| D-014 | In-process plugin interface | `IInProcessPlugin` in Core; registered in DI, no HTTP round-trip | DECIDED |
| D-015 | EF Core role | Drop entirely — Dapper + DbUp for PostgreSQL, `Microsoft.Data.Sqlite` for SQLite | DECIDED |
| D-016 | Avalonia window layout | Single window: virtual grid (main), left nav sidebar, settings page | DECIDED |

---

## Phase 1 — .NET Solution Skeleton ✅ COMPLETE

**Goal:** Compilable, empty projects with correct references. No logic yet.

**Done when:** `dotnet build` exits 0 with no warnings. Solution visible in IDE with all projects.

**Actual outcome:** `dotnet build` exits 0, 2 warnings (unfixable Avalonia transitive dep NU1903). All 7 projects created, all references wired, all packages added.

### Tasks

- [x] **Rename gitignore → .gitignore** at repository root.

- [x] **Create solution** — `PiKoRe.slnx` (`.slnx` format, .NET 10 SDK default — see D-019).

- [x] **Create projects** — all target `net10.0` (changed from plan's `net9.0` — see D-017):
  - `src/PiKoRe.Core/`, `src/PiKoRe.Data/`, `src/PiKoRe.UI/`
  - `src/PiKoRe.Plugins.Exif/`, `src/PiKoRe.Plugins.Thumbnails/`
  - `tests/PiKoRe.Core.Tests/`, `tests/PiKoRe.Data.Tests/`

- [x] **Project references** wired as planned.

- [x] **Add NuGet packages** — `Polly.Extensions.Http` was skipped (deprecated in Polly v8, see D-018). All others added as planned.

- [x] **Delete** auto-generated `Class1.cs` / `UnitTest1.cs` stubs.

- [x] **Create empty directory structure**: `Migrations/SQLite/`, `Migrations/PostgreSQL/`, `plugins/`.

- [x] **Verify**: `dotnet build` exits 0 (2 warnings — Avalonia transitive dep, not actionable).

---

## Phase 2 — Core Interfaces + DB Schemas ✅ COMPLETE

**Goal:** All public contracts in `PiKoRe.Core` defined. Both DB schemas applied by DbUp. No implementation beyond interfaces.

**Actual outcome:** `dotnet build` 0 errors. `dotnet test` 2/2 passed (Testcontainers PostgreSQL + pgvector). All interfaces, models, events, migration files, and `DatabaseMigrator` created.

### Tasks

#### 2a — Core interfaces (`src/PiKoRe.Core/`) ✅

- [x] **`Abstractions/IPlugin.cs`**
- [x] **`Abstractions/IInProcessPlugin.cs`**
- [x] **`Abstractions/IExternalPlugin.cs`**
- [x] **`Abstractions/IPluginRegistry.cs`**
- [x] **`Abstractions/IJobQueue.cs`**
- [x] **`Abstractions/IJobRunner.cs`**
- [x] **`Abstractions/IFileScanner.cs`**
- [x] **`Abstractions/IMediaStore.cs`**
- [x] **`Models/`** — `Job`, `JobResult`, `AnalysisRequest`, `AnalysisResult` (+ companion `FaceResult`), `IndexedFile`, `JobStatus`
- [x] **`Constants/Capabilities.cs`** — `Exif`, `Thumbnail`, `Embedding`, `Tags`, `Faces`, `Description`, `NsfwScore`, `AestheticScore`
- [x] **`Events/`** — `FileIndexedEvent`, `JobCompletedEvent`, `JobFailedEvent`, `PluginRegisteredEvent`

#### 2b — SQLite schema ✅

- [x] **`src/PiKoRe.Data/Migrations/SQLite/0001_initial_schema.sql`** — 5 tables + WAL mode.

#### 2c — PostgreSQL schema ✅

- [x] **`src/PiKoRe.Data/Migrations/PostgreSQL/0001_initial_schema.sql`** — 9 tables, pgvector extension, HNSW index.

#### 2d — DbUp wiring ✅

- [x] **`DatabaseMigrator.cs`** — SQL files embedded as assembly resources (see D-020). Signatures: `MigrateSqlite(string, ILogger)` and `MigratePostgres(string, ILogger)`. Throws on failure. Contains private `MicrosoftLogAdapter : IUpgradeLog` (DbUp 6.x API).
- [x] **`PiKoRe.Data.csproj`** — `<EmbeddedResource Include="Migrations/**/*.sql" />` added.
- [x] **`tests/PiKoRe.Data.Tests/DatabaseMigratorTests.cs`** — 2 tests: schema applied + idempotency. Uses `new PostgreSqlBuilder("pgvector/pgvector:pg16")` (Testcontainers 4.x constructor form — see DISCOVERIES).

---

## Phase 3 — Job Queue + Pipeline Engine

**Goal:** Working job queue backed by SQLite. `LocalSequentialRunner` executes jobs. MediatR events fire on completion. Background service drains the queue.

**Done when:** `PiKoRe.Core.Tests` verifies that enqueuing a job, running the pipeline, and receiving the `JobCompletedEvent` all work end-to-end with a stub plugin.

### Tasks

- [ ] **`Data/SqliteJobQueue.cs`** — implements `IJobQueue` using `Microsoft.Data.Sqlite`. Uses parameterised queries only. WAL mode must be set on connection open. Threads `CancellationToken`.

- [ ] **`Pipeline/LocalSequentialRunner.cs`** — implements `IJobRunner`:
  - `SemaphoreSlim _gpuSlot = new(1,1)` — one GPU job at a time
  - `SemaphoreSlim _cpuSlots = new(4,4)` — configurable N (from `IConfiguration`)
  - Calls `IInProcessPlugin.AnalyzeAsync` for in-process, `IPluginHttpClient.CallAsync` for external
  - Publishes `JobCompletedEvent` or `JobFailedEvent` via `IMediator`
  - Wraps plugin call with Polly retry (3 attempts, exponential backoff) on transient errors

- [ ] **`Pipeline/PipelineWorker.cs`** — `BackgroundService` that:
  - Polls `IJobQueue.DequeueAsync` in a loop (100ms interval — keep it simple)
  - Dispatches each job to `IJobRunner`
  - Uses structured logging: `Log.ForContext("job_id", job.Id).ForContext("capability", job.Capability)`

- [ ] **`Pipeline/DagEngine.cs`** — given a completed `AnalysisResult`, reads pipeline config from SQLite, determines which capabilities are now unblocked, enqueues new jobs. Triggered by `INotificationHandler<JobCompletedEvent>`.

- [ ] **Test**: `PiKoRe.Core.Tests/Pipeline/LocalSequentialRunnerTests.cs` — uses `NSubstitute` stubs for `IJobQueue` and an `IInProcessPlugin` that returns a fixed `AnalysisResult`. Asserts `JobCompletedEvent` is published.

---

## Phase 4 — Plugin Host + Debug Endpoint

**Goal:** Core exposes HTTP endpoints that plugins call to register. Debug endpoint at `:7701` returns live system state.

**Done when:** A manual `curl` to `:7700/api/plugins/register` with a JSON payload adds the plugin to the registry. `curl :7701/debug/plugins` returns it.

### Tasks

- [ ] **Add `Microsoft.AspNetCore.App` framework reference** to `PiKoRe.Core` or a new `PiKoRe.Host` project (decide: minimal API hosted in Core, or separate). Recommendation: separate `PiKoRe.Host` project — keeps Core free of ASP.NET dependency.

- [ ] **`PiKoRe.Host` project** (classlib → console app, net9.0):
  - Reference: `PiKoRe.Core`, `PiKoRe.Data`, all plugin projects
  - NuGet: `Microsoft.AspNetCore.App` (framework ref), `Serilog.AspNetCore`

- [ ] **Plugin registration endpoint** (`POST /api/plugins/register`):
  - Accepts `PluginRegistrationRequest` JSON (name, version, endpoint, capabilities_produced, requires_capabilities, gpu_memory_mb)
  - Calls `IPluginRegistry.RegisterAsync`
  - Publishes `PluginRegisteredEvent`
  - Returns 200 with assigned plugin ID

- [ ] **Job progress endpoint** (`POST /api/jobs/{jobId}/progress`):
  - Accepts `{ "percent": int, "message": string }`
  - Logs with `job_id` context (does not store — display only in debug endpoint)

- [ ] **Debug endpoints** (all `GET`, all bound to `:7701`):
  - `/debug/plugins` — all registered plugins + status
  - `/debug/jobs/running` — currently executing jobs
  - `/debug/jobs/queued` — queue depth per capability
  - `/debug/jobs/failed` — last 50 failures with error detail
  - `/debug/pipeline/dag` — current DAG JSON from SQLite
  - `/debug/files/{id}` — all analysis results for one file (queries PostgreSQL)
  - `POST /debug/jobs/{id}/retry` — re-enqueues a failed job
  - `POST /debug/plugins/{name}/ping` — calls plugin `GET /health`, logs result

- [ ] **`PluginHttpClient.cs`** — `IHttpClientFactory`-based client. `CallAsync(IExternalPlugin plugin, AnalysisRequest request, CancellationToken ct)` → `AnalysisResult`. Wraps with Polly retry. Logs `plugin_name`, `job_id` on every call.

---

## Phase 5 — File Scanner + Indexing

**Goal:** Given a configured library path, core recursively scans for image/video files, computes SHA-256 hashes, and writes to SQLite `file_index`. Publishes `FileIndexedEvent` for new/changed files.

**Done when:** Pointing the scanner at a real photo folder results in populated `file_index` rows. `FileIndexedEvent` fires for each new file. Duplicate scans are idempotent.

### Tasks

- [ ] **`FileScanner/FileScanner.cs`** — implements `IFileScanner`:
  - Supported extensions: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.heic`, `.mp4`, `.mov`, `.mkv`, `.avi`
  - Uses `FileSystemWatcher` for `WatchAsync`
  - Computes SHA-256 using `System.Security.Cryptography.SHA256`
  - Skips files whose `(path, mtime, size)` triple matches an existing row — no re-hash needed
  - Publishes `FileIndexedEvent` via `IMediator` for new or modified files
  - Threads `CancellationToken` through all I/O

- [ ] **Configuration**: Library path(s) read from `IConfiguration` key `"LibraryPaths"` (string array). Stored in `system_config` SQLite table after first run.

- [ ] **`FileIndexedHandler.cs`** — `INotificationHandler<FileIndexedEvent>` that enqueues one job per capability in the configured pipeline (starting with `Exif`, then `Thumbnail`).

- [ ] **Test**: `PiKoRe.Core.Tests/FileScanner/FileScannerTests.cs` — create temp directory with 3 image files, run `ScanAsync`, assert 3 rows in a mock `IJobQueue`. Run again, assert no new rows (idempotent).

---

## Phase 6 — In-Process Plugins: Exif + Thumbnail

**Goal:** `PiKoRe.Plugins.Exif` extracts EXIF metadata and writes to PostgreSQL `metadata` table. `PiKoRe.Plugins.Thumbnails` generates a 256px JPEG thumbnail and writes its path to `thumbnails` table.

**Done when:** Manually triggering the pipeline on one photo results in `metadata` rows in Postgres and a thumbnail file on disk. Visible via `/debug/files/{id}`.

### Tasks

#### ExifExtractor (`src/PiKoRe.Plugins.Exif/`)

- [ ] **`ExifPlugin.cs`** — implements `IInProcessPlugin`:
  - `Name`: `"exif-extractor"`
  - `CapabilitiesProduced`: `[Capabilities.Exif]`
  - `RequiredCapabilities`: `[]`
  - Uses `MetadataExtractor` to read all EXIF/IPTC/GPS tags
  - Calls `IMediaStore.UpsertMetadataAsync` for each tag (key=`"{directory}.{tag}"`, value=`tag.Description`)
  - Structured log: `Log.ForContext("file_id", request.FileId).ForContext("plugin", Name)`

- [ ] **Test**: One test with a real JPEG fixture file. Assert at least one `metadata` row is written (use in-memory SQLite or Testcontainers Postgres).

#### ThumbnailGenerator (`src/PiKoRe.Plugins.Thumbnails/`)

- [ ] **`ThumbnailPlugin.cs`** — implements `IInProcessPlugin`:
  - `Name`: `"thumbnail-generator"`
  - `CapabilitiesProduced`: `[Capabilities.Thumbnail]`
  - `RequiredCapabilities`: `[]`
  - Images: use `ImageSharp` to resize to max 256px on longest side, save as JPEG to `~/.config/pikore/thumbnails/{fileId}.jpg`
  - Videos: use `FFMpegCore` to extract frame at 00:00:01, then same resize
  - Calls `IMediaStore.UpsertThumbnailAsync` with size class `"256"` and data path
  - Returns `AnalysisResult` with `PreviewPath` set (allows downstream plugins to use thumbnail instead of full file)

- [ ] **Test**: One test with a real JPEG fixture. Assert thumbnail file exists on disk at expected path.

---

## Phase 7 — Avalonia UI Skeleton

**Goal:** App window shows a virtual scrolling grid of thumbnails read from the `thumbnails` Postgres table. No search yet. No selection detail. Just the grid.

**Done when:** Launching the app with photos already indexed shows their thumbnails in a responsive grid. Scrolling works without visible jank. No crashes.

### Tasks

- [ ] **`App.axaml`** layout:
  - `DockPanel` root
  - Left: `NavigationPanel` (sidebar, 200px wide) — static items: "All", "By Date", "By Tag", "Settings" (no behaviour in MVP)
  - Center: `VirtualScrollGrid` (custom control or `ItemsRepeater` with virtualization)

- [ ] **`ViewModels/MainViewModel.cs`** — `INotifyPropertyChanged`. Exposes `ObservableCollection<ThumbnailItem>`. On load, queries `IMediaStore` for first 200 thumbnails. Loads more on scroll (20 at a time).

- [ ] **`ThumbnailItem.cs`** — `FileId`, `ThumbnailPath`, `FileName`. Loaded async; image decode on background thread.

- [ ] **`Controls/ThumbnailGrid.axaml`** — `ItemsRepeater` with `UniformGridLayout`. Each cell: `Image` bound to `ThumbnailPath`, 256×256, `Stretch.UniformToFill`.

- [ ] **App wiring**: `Program.cs` builds `IHost` with all services registered. DI in `App.axaml.cs`.

---

## Phase 8 — CLIP Python Plugin

**Goal:** `plugins/clip-embedder/` is a FastAPI plugin that accepts an image path, computes a 512-dim CLIP embedding via `sentence-transformers`, writes it to the `embeddings` PostgreSQL table, and registers with core on startup.

**Done when:** Plugin starts, registers at `:7700/api/plugins/register`, processes one job triggered by the pipeline, and `/debug/files/{id}` shows an embedding row for that file.

### Tasks

- [ ] **`plugins/clip-embedder/plugin.json`**:
  ```json
  {
    "name": "clip-embedder",
    "version": "1.0.0",
    "capabilities_produced": ["embedding"],
    "requires_capabilities": ["thumbnail"],
    "endpoint": "http://localhost:5001",
    "gpu_memory_mb": 800,
    "startup_command": null
  }
  ```

- [ ] **`plugins/clip-embedder/plugin.py`** — FastAPI app:
  - `GET /health` — returns `{"status":"ok"}`
  - `POST /analyze` — receives `AnalysisRequest` JSON, loads image from `preview_path`, runs CLIP, returns embedding as `float[]`
  - On startup: `POST http://localhost:7700/api/plugins/register` with manifest data (retry with backoff)
  - Writes embedding directly to Postgres via `asyncpg` + `pgvector`
  - Structured logging: include `job_id` and `file_id` in every log line

- [ ] **`plugins/clip-embedder/requirements.txt`**:
  ```
  fastapi==0.115.0
  uvicorn[standard]==0.32.0
  sentence-transformers==3.2.0
  Pillow==11.0.0
  numpy==2.1.0
  asyncpg==0.30.0
  pgvector==0.3.5
  httpx==0.27.0
  ```

- [ ] **`plugins/clip-embedder/install.sh`**:
  ```bash
  #!/usr/bin/env bash
  python3 -m venv .venv
  .venv/bin/pip install -r requirements.txt
  echo "clip-embedder installed successfully."
  ```

---

## Phase 9 — Text Search

**Goal:** Search bar in the UI sends a CLIP text query to the backend, which embeds the query and returns the top-20 visually matching files via pgvector ANN.

**Done when:** Typing "sunset beach" in the search bar shows relevant photos within 1 second.

### Tasks

- [ ] **`IMediaStore.SearchByEmbeddingAsync`** (already in interface) — implement in `PostgresMediaStore.cs` using Dapper:
  ```sql
  SELECT file_id FROM embeddings
  ORDER BY vector <=> @queryVector LIMIT @limit
  ```

- [ ] **Text-to-embedding endpoint**: A lightweight in-process helper that calls the CLIP Python plugin's `POST /embed-text` endpoint (add this endpoint to Phase 8 plugin) and returns a `float[]`. Alternatively, add a `sentence-transformers` CLIP text encoder as a second in-process plugin — simpler.

  Recommendation: add `POST /embed-text` to the CLIP plugin (same process, same model, just encodes text instead of image). Core calls it via `PluginHttpClient`.

- [ ] **`MainViewModel.SearchCommand`** — `ICommand` that:
  1. Calls text embedding
  2. Calls `IMediaStore.SearchByEmbeddingAsync`
  3. Updates `ObservableCollection<ThumbnailItem>` with results

- [ ] **Search bar in UI**: `TextBox` + search button in the top bar. Bound to `MainViewModel.SearchCommand`.

---

## Phase 10 — MVP Polish + Integration Test

**Goal:** End-to-end: configure folder → scan → thumbnails appear → CLIP runs → text search returns results. All phases working together.

**Done when:** Fresh machine setup (docker compose up, install.sh, dotnet run) achieves the MVP steps in `docs/architecture.md` §MVP.

### Tasks

- [ ] **appsettings.json** with sane defaults: DB connection strings, port numbers, library paths placeholder.
- [ ] **README.md** quick-start: prerequisites, `docker compose up -d`, `install.sh` for each plugin, `dotnet run`.
- [ ] **Logging verification**: confirm Seq at `:8081` receives structured events with `file_id`, `plugin_name`, `job_id` context.
- [ ] **Error recovery test**: kill the CLIP plugin mid-job. Verify job marked `Failed`. Verify `/debug/jobs/retry` re-runs it. Verify no crash in core.
- [ ] **Idempotency test**: scan same folder twice. Verify no duplicate `file_index` rows, no duplicate jobs.
- [ ] **Performance baseline**: 100-photo folder. Time from scan-start to all thumbnails visible. Target: < 60 seconds on dev machine.

---

## Notes for agents

- Always check CURRENT_STATE.md before starting. The plan above may be ahead of or behind actual state.
- Never modify files in `src/PiKoRe.Data/Migrations/` once committed — only add new numbered files.
- Never add `new HttpClient()` — use `IHttpClientFactory`.
- Never use `.Result` or `.Wait()` — async all the way down.
- Every async method touching I/O takes `CancellationToken ct` as last parameter.
- No `Console.WriteLine` in production code — use `Serilog`.
- After completing any phase, update CURRENT_STATE.md and append any new decisions to DECISIONS.md.
