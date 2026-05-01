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

## Phase 1 — .NET Solution Skeleton

**Goal:** Compilable, empty projects with correct references. No logic yet.

**Done when:** `dotnet build` exits 0 with no warnings. Solution visible in IDE with all projects.

### Tasks

- [ ] **Rename gitignore → .gitignore** at repository root (currently named `gitignore`, not picked up by git).

- [ ] **Create solution**
  ```
  dotnet new sln -n PiKoRe
  ```

- [ ] **Create projects** (all target `net9.0`):
  ```
  dotnet new classlib -n PiKoRe.Core        -o src/PiKoRe.Core
  dotnet new classlib -n PiKoRe.Data        -o src/PiKoRe.Data
  dotnet new avalonia -n PiKoRe.UI          -o src/PiKoRe.UI          --framework net9.0
  dotnet new classlib -n PiKoRe.Plugins.Exif      -o src/PiKoRe.Plugins.Exif
  dotnet new classlib -n PiKoRe.Plugins.Thumbnails -o src/PiKoRe.Plugins.Thumbnails
  dotnet new xunit    -n PiKoRe.Core.Tests  -o tests/PiKoRe.Core.Tests
  dotnet new xunit    -n PiKoRe.Data.Tests  -o tests/PiKoRe.Data.Tests
  ```
  Add all to solution:
  ```
  dotnet sln add src/**/*.csproj tests/**/*.csproj
  ```

- [ ] **Project references**:
  - `PiKoRe.Data` → `PiKoRe.Core`
  - `PiKoRe.UI` → `PiKoRe.Core`, `PiKoRe.Data`
  - `PiKoRe.Plugins.Exif` → `PiKoRe.Core`
  - `PiKoRe.Plugins.Thumbnails` → `PiKoRe.Core`
  - `PiKoRe.Core.Tests` → `PiKoRe.Core`
  - `PiKoRe.Data.Tests` → `PiKoRe.Data`, `PiKoRe.Core`

- [ ] **Add NuGet packages** to each project:

  *PiKoRe.Core*:
  - `Microsoft.Extensions.Hosting.Abstractions`
  - `MediatR`
  - `Serilog`
  - `Serilog.Extensions.Hosting`
  - `OpenTelemetry`
  - `OpenTelemetry.Api`
  - `Polly`
  - `Polly.Extensions.Http`

  *PiKoRe.Data*:
  - `Npgsql`
  - `Dapper`
  - `Microsoft.Data.Sqlite`
  - `DbUp-Core`
  - `DbUp-PostgreSQL`
  - `DbUp-SQLite`

  *PiKoRe.UI*:
  - `Avalonia`
  - `Avalonia.Desktop`
  - `Avalonia.Themes.Fluent`
  - `Serilog.Sinks.Console`
  - `Serilog.Sinks.File`
  - `Serilog.Sinks.Seq`
  - `OpenTelemetry.Extensions.Hosting`
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol`

  *PiKoRe.Plugins.Exif*:
  - `MetadataExtractor`

  *PiKoRe.Plugins.Thumbnails*:
  - `SixLabors.ImageSharp`
  - `FFMpegCore`

  *PiKoRe.Core.Tests*, *PiKoRe.Data.Tests*:
  - `xunit`
  - `xunit.runner.visualstudio`
  - `NSubstitute`
  - `Testcontainers.PostgreSql` (Data.Tests only)

- [ ] **Delete** auto-generated `Class1.cs` placeholder files from all classlib projects.

- [ ] **Create empty directory structure**:
  ```
  src/PiKoRe.Data/Migrations/SQLite/
  src/PiKoRe.Data/Migrations/PostgreSQL/
  plugins/             (empty, placeholder for external plugins)
  ```

- [ ] **Verify**: `dotnet build` exits 0.

---

## Phase 2 — Core Interfaces + DB Schemas

**Goal:** All public contracts in `PiKoRe.Core` defined. Both DB schemas applied by DbUp. No implementation beyond interfaces.

**Done when:** `dotnet build` succeeds. `dotnet test` passes (trivially — no logic yet). DbUp can run against a live Postgres + SQLite and apply initial schema with no errors.

### Tasks

#### 2a — Core interfaces (`src/PiKoRe.Core/`)

- [ ] **`Abstractions/IPlugin.cs`** — base interface for all plugins (in-process and external):
  ```csharp
  public interface IPlugin
  {
      string Name { get; }
      string Version { get; }
      IReadOnlyList<string> CapabilitiesProduced { get; }
      IReadOnlyList<string> RequiredCapabilities { get; }
  }
  ```

- [ ] **`Abstractions/IInProcessPlugin.cs`** — interface for C# plugins running in-process:
  ```csharp
  public interface IInProcessPlugin : IPlugin
  {
      Task<AnalysisResult> AnalyzeAsync(AnalysisRequest request, CancellationToken ct);
  }
  ```

- [ ] **`Abstractions/IExternalPlugin.cs`** — represents a registered external (HTTP) plugin:
  ```csharp
  public interface IExternalPlugin : IPlugin
  {
      Uri Endpoint { get; }
      int GpuMemoryMb { get; }
  }
  ```

- [ ] **`Abstractions/IPluginRegistry.cs`**:
  ```csharp
  public interface IPluginRegistry
  {
      Task RegisterAsync(IExternalPlugin plugin, CancellationToken ct);
      Task DeregisterAsync(string name, CancellationToken ct);
      Task<IReadOnlyList<IPlugin>> GetAllAsync(CancellationToken ct);
      Task<IPlugin?> GetByCapabilityAsync(string capability, CancellationToken ct);
  }
  ```

- [ ] **`Abstractions/IJobQueue.cs`**:
  ```csharp
  public interface IJobQueue
  {
      Task EnqueueAsync(Job job, CancellationToken ct);
      Task<Job?> DequeueAsync(CancellationToken ct);
      Task MarkCompletedAsync(Guid jobId, CancellationToken ct);
      Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct);
  }
  ```

- [ ] **`Abstractions/IJobRunner.cs`**:
  ```csharp
  public interface IJobRunner
  {
      Task<JobResult> RunAsync(Job job, CancellationToken ct);
  }
  ```

- [ ] **`Abstractions/IFileScanner.cs`**:
  ```csharp
  public interface IFileScanner
  {
      Task ScanAsync(string libraryPath, CancellationToken ct);
      IAsyncEnumerable<IndexedFile> WatchAsync(string libraryPath, CancellationToken ct);
  }
  ```

- [ ] **`Abstractions/IMediaStore.cs`**:
  ```csharp
  public interface IMediaStore
  {
      Task<IndexedFile?> GetByIdAsync(Guid fileId, CancellationToken ct);
      Task<IReadOnlyList<IndexedFile>> SearchByEmbeddingAsync(float[] queryVector, int limit, CancellationToken ct);
      Task UpsertMetadataAsync(Guid fileId, string key, string value, string sourcePlugin, CancellationToken ct);
      Task UpsertTagAsync(Guid fileId, string label, float confidence, string sourcePlugin, CancellationToken ct);
      Task UpsertEmbeddingAsync(Guid fileId, string modelId, float[] vector, CancellationToken ct);
      Task UpsertDescriptionAsync(Guid fileId, string text, string sourcePlugin, CancellationToken ct);
  }
  ```

- [ ] **`Models/`** — record types (no logic):
  - `Job.cs` — `Guid Id`, `Guid FileId`, `string Capability`, `JobStatus Status`, `Guid? PluginId`, `int Priority`, `DateTimeOffset Created`, `DateTimeOffset Updated`, `string? Error`
  - `JobResult.cs` — `Guid JobId`, `bool Success`, `string? Error`, `AnalysisResult? Result`
  - `AnalysisRequest.cs` — `Guid JobId`, `Guid FileId`, `string FilePath`, `string? PreviewPath`
  - `AnalysisResult.cs` — nullable collections: `Tags`, `Faces`, `float[]? Embedding`, `Dictionary<string,float>? Scores`, `string? Description`
  - `IndexedFile.cs` — `Guid Id`, `string Path`, `long SizeBytes`, `DateTimeOffset MTime`, `string Hash`, `DateTimeOffset IngestedAt`
  - `JobStatus.cs` — enum: `Queued`, `Running`, `Completed`, `Failed`

- [ ] **`Constants/Capabilities.cs`** — static class with `public const string` for each capability: `Exif`, `Thumbnail`, `Embedding`, `Tags`, `Faces`, `Description`, `NsfwScore`, `AestheticScore`

- [ ] **`Events/`** — MediatR `INotification` records:
  - `FileIndexedEvent.cs` — `IndexedFile File`
  - `JobCompletedEvent.cs` — `JobResult Result`
  - `JobFailedEvent.cs` — `Guid JobId`, `string Error`
  - `PluginRegisteredEvent.cs` — `string PluginName`

#### 2b — SQLite schema (`src/PiKoRe.Data/Migrations/SQLite/`)

- [ ] **`0001_initial_schema.sql`**:
  ```sql
  CREATE TABLE IF NOT EXISTS plugin_registry (
      id          TEXT PRIMARY KEY,
      name        TEXT NOT NULL UNIQUE,
      version     TEXT NOT NULL,
      endpoint    TEXT,
      capabilities_produced TEXT NOT NULL,  -- JSON array
      required_capabilities TEXT NOT NULL,  -- JSON array
      gpu_memory_mb INTEGER NOT NULL DEFAULT 0,
      status      TEXT NOT NULL DEFAULT 'active',
      config_json TEXT,
      registered_at TEXT NOT NULL
  );

  CREATE TABLE IF NOT EXISTS file_index (
      id          TEXT PRIMARY KEY,
      path        TEXT NOT NULL UNIQUE,
      size_bytes  INTEGER NOT NULL,
      mtime       TEXT NOT NULL,
      hash        TEXT NOT NULL,
      ingested_at TEXT NOT NULL
  );

  CREATE TABLE IF NOT EXISTS job_queue (
      id          TEXT PRIMARY KEY,
      file_id     TEXT NOT NULL,
      capability  TEXT NOT NULL,
      status      TEXT NOT NULL DEFAULT 'queued',
      plugin_id   TEXT,
      priority    INTEGER NOT NULL DEFAULT 0,
      created_at  TEXT NOT NULL,
      updated_at  TEXT NOT NULL,
      error       TEXT,
      FOREIGN KEY (file_id) REFERENCES file_index(id)
  );

  CREATE TABLE IF NOT EXISTS pipeline_config (
      id          TEXT PRIMARY KEY,
      dag_json    TEXT NOT NULL,
      updated_at  TEXT NOT NULL
  );

  CREATE TABLE IF NOT EXISTS system_config (
      key         TEXT PRIMARY KEY,
      value       TEXT NOT NULL
  );

  PRAGMA journal_mode=WAL;
  ```

#### 2c — PostgreSQL schema (`src/PiKoRe.Data/Migrations/PostgreSQL/`)

- [ ] **`0001_initial_schema.sql`**:
  ```sql
  CREATE EXTENSION IF NOT EXISTS vector;

  CREATE TABLE IF NOT EXISTS thumbnails (
      file_id     UUID NOT NULL,
      size_class  TEXT NOT NULL,
      data_path   TEXT NOT NULL,
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
      PRIMARY KEY (file_id, size_class)
  );

  CREATE TABLE IF NOT EXISTS metadata (
      id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      file_id     UUID NOT NULL,
      key         TEXT NOT NULL,
      value       TEXT NOT NULL,
      source_plugin TEXT NOT NULL,
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
      UNIQUE (file_id, key, source_plugin)
  );

  CREATE TABLE IF NOT EXISTS tags (
      id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      file_id     UUID NOT NULL,
      label       TEXT NOT NULL,
      confidence  REAL NOT NULL,
      source_plugin TEXT NOT NULL,
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
      UNIQUE (file_id, label, source_plugin)
  );

  CREATE TABLE IF NOT EXISTS embeddings (
      file_id     UUID NOT NULL,
      model_id    TEXT NOT NULL,
      vector      vector(512),
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
      PRIMARY KEY (file_id, model_id)
  );

  CREATE TABLE IF NOT EXISTS faces (
      id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      file_id     UUID NOT NULL,
      bbox_json   TEXT NOT NULL,
      embedding   vector(512),
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
  );

  CREATE TABLE IF NOT EXISTS persons (
      id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      name            TEXT NOT NULL,
      cover_face_id   UUID,
      created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
  );

  CREATE TABLE IF NOT EXISTS face_person (
      face_id     UUID NOT NULL,
      person_id   UUID NOT NULL,
      confidence  REAL NOT NULL DEFAULT 1.0,
      PRIMARY KEY (face_id, person_id)
  );

  CREATE TABLE IF NOT EXISTS scores (
      file_id     UUID NOT NULL,
      key         TEXT NOT NULL,
      value       REAL NOT NULL,
      source_plugin TEXT NOT NULL,
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
      PRIMARY KEY (file_id, key, source_plugin)
  );

  CREATE TABLE IF NOT EXISTS descriptions (
      id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      file_id     UUID NOT NULL,
      text        TEXT NOT NULL,
      source_plugin TEXT NOT NULL,
      created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
  );

  CREATE INDEX IF NOT EXISTS idx_embeddings_vector ON embeddings USING hnsw (vector vector_cosine_ops);
  CREATE INDEX IF NOT EXISTS idx_tags_file_id ON tags (file_id);
  CREATE INDEX IF NOT EXISTS idx_metadata_file_id ON metadata (file_id);
  CREATE INDEX IF NOT EXISTS idx_faces_file_id ON faces (file_id);
  ```

#### 2d — DbUp wiring (`src/PiKoRe.Data/`)

- [ ] **`DatabaseMigrator.cs`** — static class with `MigrateSqlite(string connectionString)` and `MigratePostgres(string connectionString)`. Each scans the appropriate `Migrations/` folder using DbUp. Throws on failure. Logs to `ILogger` passed in.

- [ ] **Test**: One test in `PiKoRe.Data.Tests` that spins up a Testcontainers PostgreSQL instance and runs `MigratePostgres`. Assert no exception. Assert `embeddings` table exists.

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
