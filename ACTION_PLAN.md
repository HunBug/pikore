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

**Actual outcome:** `dotnet build` 0 errors. `dotnet test` 2/2 passed (Testcontainers PostgreSQL + pgvector). All interfaces, models, events, migration files, and `DatabaseMigrator` created. Post-phase addendum applied (D-023, D-024, D-025): `SupportedMediaTypes` added to `IPlugin` and `ExternalPluginInfo`; `MediaType` added to `IndexedFile`; `MediaTypes` constants class created; SQLite migration `0002_add_media_type_columns.sql` added.

### Tasks

#### 2a — Core interfaces (`src/PiKoRe.Core/`) ✅

- [x] **`Abstractions/IPlugin.cs`** — includes `SupportedMediaTypes: IReadOnlyList<string>` (see D-024)
- [x] **`Abstractions/IInProcessPlugin.cs`**
- [x] **`Abstractions/IPluginRegistry.cs`**
- [x] **`Abstractions/IJobQueue.cs`**
- [x] **`Abstractions/IJobRunner.cs`**
- [x] **`Abstractions/IFileScanner.cs`**
- [x] **`Abstractions/IMediaStore.cs`**
- [x] **`Models/`** — `Job`, `JobResult`, `AnalysisRequest`, `AnalysisResult` (+ `FaceResult`), `IndexedFile` (includes `MediaType`), `JobStatus`, `ExternalPluginInfo` (includes `SupportedMediaTypes`)
- [x] **`Constants/Capabilities.cs`** — `Exif`, `Thumbnail`, `Embedding`, `Tags`, `Faces`, `Description`, `NsfwScore`, `AestheticScore`
- [x] **`Constants/MediaTypes.cs`** — `Image`, `Video`, `Audio`, `All` constants; `FromExtension(string)` helper; `IsSupported(string, IReadOnlyList<string>)` helper
- [x] **`Events/`** — `FileIndexedEvent`, `JobCompletedEvent`, `JobFailedEvent`, `PluginRegisteredEvent`

#### 2b — SQLite schema ✅

- [x] **`src/PiKoRe.Data/Migrations/SQLite/0001_initial_schema.sql`** — 5 tables + WAL mode.
- [x] **`src/PiKoRe.Data/Migrations/SQLite/0002_add_media_type_columns.sql`** — adds `media_type` to `file_index`; adds `supported_media_types` to `plugin_registry`.

#### 2c — PostgreSQL schema ✅

- [x] **`src/PiKoRe.Data/Migrations/PostgreSQL/0001_initial_schema.sql`** — 9 tables, pgvector extension, HNSW index.

#### 2d — DbUp wiring ✅

- [x] **`DatabaseMigrator.cs`** — SQL files embedded as assembly resources (see D-020). Signatures: `MigrateSqlite(string, ILogger)` and `MigratePostgres(string, ILogger)`. Throws on failure.
- [x] **`PiKoRe.Data.csproj`** — `<EmbeddedResource Include="Migrations/**/*.sql" />` added.
- [x] **`tests/PiKoRe.Data.Tests/DatabaseMigratorTests.cs`** — 2 tests: schema applied + idempotency.

---

## Phase 3 — Job Queue + Pipeline Engine ✅ COMPLETE

**Goal:** Working job queue backed by SQLite. `LocalSequentialRunner` executes jobs. MediatR events fire on completion. Background service drains the queue. DagEngine chains capabilities based on `pipeline_config` and plugin declarations.

**Actual outcome:** `dotnet build` 0 errors. `dotnet test` 4/4 passed (2 new Core pipeline tests + 2 existing Data tests). All model changes, `SqliteJobQueue`, and pipeline components created.

### Model changes (apply before implementing)

- [x] **`Models/Job.cs`** — add `FilePath` and `MediaType` fields. Both are populated via JOIN with `file_index` in `DequeueAsync`; no schema change needed (they live in `file_index`).

- [x] **`Models/JobResult.cs`** — add `FileId`, `Capability`, and `MediaType` fields (see D-026). `FileId` and `Capability` required by `DagEngine` to determine which capability completed for which file. `MediaType` required by `DagEngine` to check plugin compatibility without a second DB round-trip — the event fires before `PipelineWorker` calls `MarkCompletedAsync`, so the DB row is still `running` at handler time.

- [x] **`Abstractions/IJobQueue.cs`** — add three methods needed by `DagEngine`:
  - `Task<IReadOnlyList<string>> GetCompletedCapabilitiesForFileAsync(Guid fileId, CancellationToken ct)`
  - `Task<bool> JobExistsForFileAndCapabilityAsync(Guid fileId, string capability, CancellationToken ct)`
  - `Task<string?> GetPipelineConfigDagJsonAsync(CancellationToken ct)`

### Tasks

- [x] **`Data/SqliteJobQueue.cs`** — implements `IJobQueue` using Dapper + `Microsoft.Data.Sqlite`. Reads connection string from `IConfiguration["ConnectionStrings:SQLite"]`. Each method opens a new connection and executes `PRAGMA journal_mode=WAL`. Uses parameterised queries only. `DequeueAsync` uses `BEGIN IMMEDIATE` (raw SQL) for atomic select + update.
  - `EnqueueAsync` — INSERT into `job_queue` (FilePath/MediaType are NOT stored — populated at dequeue time via JOIN)
  - `DequeueAsync` — `BEGIN IMMEDIATE` → SELECT + JOIN `file_index` to populate `FilePath` and `MediaType` → UPDATE status to `running` → `COMMIT`
  - `MarkCompletedAsync` / `MarkFailedAsync` — UPDATE status + `updated_at` (+ `error`)
  - `GetCompletedCapabilitiesForFileAsync` — SELECT capability WHERE file\_id=@id AND status='completed'
  - `JobExistsForFileAndCapabilityAsync` — SELECT COUNT(\*) WHERE file\_id=@id AND capability=@cap AND status NOT IN ('failed')
  - `GetPipelineConfigDagJsonAsync` — SELECT dag\_json FROM pipeline\_config ORDER BY updated\_at DESC LIMIT 1

- [x] **`Pipeline/LocalSequentialRunner.cs`** — implements `IJobRunner`:
  - `SemaphoreSlim _gpuSlot = new(1,1)` — reserved; all Phase 3 jobs use CPU slots
  - `SemaphoreSlim _cpuSlots = new(N,N)` — N from `int.TryParse(config["Pipeline:MaxCpuSlots"], ...) ?? 4`
  - Resolves plugin via `IEnumerable<IInProcessPlugin>` — picks first whose `CapabilitiesProduced` contains `job.Capability` AND `SupportedMediaTypes` matches `job.MediaType` (use `MediaTypes.IsSupported`)
  - No plugin found → fail job with clear error message; publish `JobFailedEvent`; no silent swallow
  - Wraps `plugin.AnalyzeAsync` with Polly `ResiliencePipelineBuilder` retry (3 attempts, exponential backoff, 500ms base, jitter)
  - Success → publish `JobCompletedEvent`; exception after retries → publish `JobFailedEvent`
  - Structured log: `_logger.ForContext("job_id", ...).ForContext("capability", ...).ForContext("media_type", ...)`

- [x] **`Pipeline/PipelineWorker.cs`** — `BackgroundService`:
  - Polls `IJobQueue.DequeueAsync` in a loop; 100ms delay when queue is empty
  - Dispatches to `IJobRunner.RunAsync`; calls `MarkCompletedAsync` or `MarkFailedAsync` based on `JobResult.Success`
  - Structured log per job with `job_id` and `capability`

- [x] **`Pipeline/DagEngine.cs`** — `INotificationHandler<JobCompletedEvent>`:
  - Reads enabled capability list from `IJobQueue.GetPipelineConfigDagJsonAsync()` (JSON array of strings, see D-023); returns early if null
  - Adds `notification.Result.Capability` to the completed set before querying DB — guards against the ordering issue where `PipelineWorker` hasn't yet called `MarkCompletedAsync` when this handler runs
  - For each enabled capability not yet queued or completed for this file:
    1. Find the registered `IInProcessPlugin` that produces it and supports `notification.Result.MediaType` (from `JobResult` — see D-026)
    2. Check all of that plugin's `RequiredCapabilities` are in the completed set (via `GetCompletedCapabilitiesForFileAsync`)
    3. If unblocked and no duplicate (via `JobExistsForFileAndCapabilityAsync`) → `EnqueueAsync`
  - Structured log with `file_id`, `completed_capability`, `unblocked_capabilities`

- [x] **Test: `PiKoRe.Core.Tests/Pipeline/LocalSequentialRunnerTests.cs`**:
  - `RunAsync_PluginFound_PublishesJobCompletedEvent` — NSubstitute stub plugin returns fixed `AnalysisResult`; assert `JobCompletedEvent` published and `result.Success == true`
  - `RunAsync_NoPlugin_PublishesJobFailedEvent` — no plugins registered; assert `JobFailedEvent` published and `result.Success == false`

---

## Phase 4 — Plugin Host + Debug Endpoint

**Goal:** Core exposes HTTP endpoints that plugins call to register. Debug endpoint at `:7701` returns live system state.

**Done when:** A manual `curl` to `:7700/api/plugins/register` with a JSON payload adds the plugin to the registry. `curl :7701/debug/plugins` returns it.

### Tasks

- [ ] **Create `PiKoRe.Host` project** (console app, net10.0):
  - Add to `PiKoRe.slnx`
  - `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
  - Project references: `PiKoRe.Core`, `PiKoRe.Data`, `PiKoRe.Plugins.Exif`, `PiKoRe.Plugins.Thumbnails`
  - NuGet: `Serilog.AspNetCore`, `Microsoft.Extensions.Http.Polly`

- [ ] **`Program.cs` — DI wiring and host startup**:
  - `builder.UseSerilog(...)` — Serilog as the .NET `ILogger` provider
  - MediatR: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(DagEngine).Assembly, typeof(Program).Assembly))` — must cover both Core and Host assemblies
  - `services.AddSingleton<IJobQueue, SqliteJobQueue>()`
  - `services.AddSingleton<IPluginRegistry, SqlitePluginRegistry>()`
  - `services.AddSingleton<IJobRunner, LocalSequentialRunner>()`
  - In-process plugins (order matters — first match wins in LocalSequentialRunner):
    `services.AddSingleton<IInProcessPlugin, ExifPlugin>()`
    `services.AddSingleton<IInProcessPlugin, ThumbnailPlugin>()`
  - `services.AddHostedService<PipelineWorker>()`
  - Kestrel two-port binding (see port strategy decision):
    ```csharp
    builder.WebHost.ConfigureKestrel(o => {
        o.Listen(IPAddress.Loopback, 7700);
        o.Listen(IPAddress.Loopback, 7701);
    });
    ```
  - `IHttpClientFactory` for adapter plugins: `services.AddHttpClient()`
  - DB migration on startup: call `DatabaseMigrator.MigrateSqlite(...)` and `DatabaseMigrator.MigratePostgres(...)` before `app.Run()`

- [ ] **Port routing** — use `RequireHost` on route groups (ASP.NET Core 8+ pattern):
  ```csharp
  app.MapGroup("/api").RequireHost("*:7700").MapPluginEndpoints();
  app.MapGroup("/debug").RequireHost("*:7701").MapDebugEndpoints();
  ```
  This enforces port separation at the routing layer, not middleware. Avoids the `UseWhen(ctx.Connection.LocalPort == ...)` anti-pattern which is soft and unreliable.

- [ ] **Plugin registration endpoint** (`POST /api/plugins/register`):
  - Accepts `PluginRegistrationRequest` JSON — name, version, endpoint, capabilities\_produced, required\_capabilities, **supported\_media\_types**, gpu\_memory\_mb
  - Maps to `ExternalPluginInfo` (all fields including `SupportedMediaTypes`)
  - Calls `IPluginRegistry.RegisterAsync`; publishes `PluginRegisteredEvent`; returns 200 with assigned plugin ID

- [ ] **Job progress endpoint** (`POST /api/jobs/{jobId}/progress`):
  - Accepts `{ "percent": int, "message": string }`; logs with `job_id` context (display only)

- [ ] **Debug endpoints** (route group bound to `:7701` via `RequireHost`):
  - `GET /debug/plugins` — all registered plugins + status + supported\_media\_types
  - `GET /debug/jobs/running` — currently executing jobs
  - `GET /debug/jobs/queued` — queue depth per capability
  - `GET /debug/jobs/failed` — last 50 failures with error detail
  - `GET /debug/pipeline/dag` — current enabled capability list from `pipeline_config`
  - `GET /debug/files/{id}` — combines `IFileIndexStore.GetByIdAsync` (path/mtime/media_type from SQLite) + `IMediaStore.GetFileDetailsAsync` (metadata/tags/thumbnail/embedding from PostgreSQL) into one JSON response
  - `POST /debug/jobs/{id}/retry` — re-enqueues a failed job
  - `POST /debug/plugins/{name}/ping` — calls plugin `GET /health` via `IHttpClientFactory`, logs result

- [ ] **`SqlitePluginRegistry.cs`** — implements `IPluginRegistry`. Stores/reads `ExternalPluginInfo` records (including `SupportedMediaTypes` as JSON) from `plugin_registry`.

---

## Phase 5 — File Scanner + Indexing

**Goal:** Given configured library paths, core recursively scans for image, video, and audio files, computes SHA-256 hashes, writes to SQLite `file_index` with correct `media_type`, and publishes `FileIndexedEvent`.

**Done when:** Pointing the scanner at a folder with mixed media results in populated `file_index` rows with correct `media_type` values. `FileIndexedEvent` fires per new file. Duplicate scans are idempotent.

### Pre-requisite (define before implementing FileScanner)

- [ ] **`Abstractions/IFileIndexStore.cs`** (Core) — interface for SQLite `file_index` operations. `FileScanner` depends on this; `SqliteJobQueue` does NOT handle file indexing.
  - `Task<IndexedFile?> GetByPathAsync(string path, CancellationToken ct)` — used for skip-if-unchanged check
  - `Task UpsertAsync(IndexedFile file, CancellationToken ct)` — insert or update `file_index` row
  - `Task<IndexedFile?> GetByIdAsync(Guid fileId, CancellationToken ct)` — used by debug endpoint + DagEngine context
- [ ] **`Data/SqliteFileIndexStore.cs`** (Data) — implements `IFileIndexStore`. Same pattern as `SqliteJobQueue`: `IConfiguration["ConnectionStrings:SQLite"]`, Dapper, WAL pragma per connection.
- [ ] Register `IFileIndexStore → SqliteFileIndexStore` as singleton in `PiKoRe.Host/Program.cs` (add alongside other Phase 4 registrations, or defer to Phase 5 startup).

### Tasks

- [ ] **`FileScanner/FileScanner.cs`** — implements `IFileScanner`:
  - Supported extensions (see D-025):
    - Images: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.heic`, `.heif`, `.tiff`, `.bmp`
    - Videos: `.mp4`, `.mov`, `.mkv`, `.avi`, `.webm`, `.m4v`
    - Audio: `.mp3`, `.flac`, `.wav`, `.aac`, `.m4a`, `.ogg`, `.opus`, `.wma`
  - Uses `MediaTypes.FromExtension(ext)` to determine and store `MediaType` on each `IndexedFile`
  - Skips files where `(path, mtime, size)` triple matches an existing row — no re-hash
  - Computes SHA-256 using `System.Security.Cryptography.SHA256`
  - Uses `FileSystemWatcher` for `WatchAsync`
  - Publishes `FileIndexedEvent` (carries `IndexedFile` including `MediaType`) via `IMediator`
  - Threads `CancellationToken` through all I/O

- [ ] **Configuration**: Library path(s) from `IConfiguration["LibraryPaths"]` (string array). Stored in `system_config` after first run.

- [ ] **`FileIndexedHandler.cs`** — `INotificationHandler<FileIndexedEvent>`:
  - Gets enabled capability list from `IJobQueue.GetPipelineConfigDagJsonAsync()`
  - For each enabled capability whose plugin has empty `RequiredCapabilities` AND `SupportedMediaTypes` matches the file's `MediaType`: enqueue a new `Job`
  - When constructing `Job` to enqueue, pass `null` for `FilePath` and `null` for `MediaType` — both are populated by `DequeueAsync` via JOIN with `file_index`, not stored in `job_queue`
  - This ensures audio files don't get thumbnail/embedding jobs, and image-only capabilities don't fire for videos

- [ ] **Test**: `PiKoRe.Core.Tests/FileScanner/FileScannerTests.cs` — temp directory with 1 JPEG + 1 MP3 + 1 MP4. Run `ScanAsync`. Assert 3 rows with correct `MediaType` values. Assert no new rows on second scan (idempotent).

---

## Phase 6 — In-Process Plugins: Exif + Thumbnail

**Goal:** `PiKoRe.Plugins.Exif` extracts metadata from images, videos, and audio. `PiKoRe.Plugins.Thumbnails` generates previews for images and videos (audio gets no thumbnail in MVP).

**Done when:** Triggering the pipeline on a JPEG produces `metadata` rows in Postgres and a thumbnail on disk. Triggering on an MP3 produces `metadata` rows but no thumbnail job (correct via `SupportedMediaTypes` filtering). Visible via `/debug/files/{id}`.

### Tasks

#### ExifExtractor (`src/PiKoRe.Plugins.Exif/`)

- [ ] **`ExifPlugin.cs`** — implements `IInProcessPlugin`:
  - `Name`: `"exif-extractor"`
  - `CapabilitiesProduced`: `[Capabilities.Exif]`
  - `RequiredCapabilities`: `[]`
  - `SupportedMediaTypes`: `[MediaTypes.Image, MediaTypes.Video, MediaTypes.Audio]` — MetadataExtractor handles EXIF, ID3, MP4/container tags
  - Uses `MetadataExtractor` to read all tags; calls `IMediaStore.UpsertMetadataAsync` per tag (key=`"{directory}.{tag}"`)
  - Structured log: `file_id`, `plugin`, `media_type`

- [ ] **Test**: JPEG fixture → assert metadata rows written. MP3 fixture → assert ID3 tags written.

#### ThumbnailGenerator (`src/PiKoRe.Plugins.Thumbnails/`)

- [ ] **`ThumbnailPlugin.cs`** — implements `IInProcessPlugin`:
  - `Name`: `"thumbnail-generator"`
  - `CapabilitiesProduced`: `[Capabilities.Thumbnail]`
  - `RequiredCapabilities`: `[]`
  - `SupportedMediaTypes`: `[MediaTypes.Image, MediaTypes.Video]` — audio is excluded; DagEngine will not enqueue thumbnail jobs for audio files
  - Images: `ImageSharp` resize to max 256px on longest side, JPEG output to `~/.config/pikore/thumbnails/{fileId}.jpg`
  - Videos: `FFMpegCore` frame extract at 00:00:01, then same resize
  - Calls `IMediaStore.UpsertThumbnailAsync` with size class `"256"` and data path
  - Returns `AnalysisResult` with `PreviewPath` set

- [ ] **Test**: JPEG fixture → thumbnail file exists. Video fixture → thumbnail file exists.

---

## Phase 7 — Avalonia UI Skeleton

**Goal:** App window shows a virtual scrolling grid of thumbnails. No search yet.

**Done when:** Launching the app with photos already indexed shows thumbnails in a responsive grid without jank or crashes.

### Tasks

- [ ] **`App.axaml`** layout:
  - `DockPanel` root
  - Left: `NavigationPanel` (200px) — static items: All, By Date, By Tag, Settings (no behaviour in MVP)
  - Center: `VirtualScrollGrid`

- [ ] **`ViewModels/MainViewModel.cs`** — `INotifyPropertyChanged`. `ObservableCollection<ThumbnailItem>`. Loads first 200 thumbnails on startup; loads 20 more on scroll.

- [ ] **`ThumbnailItem.cs`** — `FileId`, `ThumbnailPath`, `FileName`, `MediaType`. Image decode on background thread.

- [ ] **`Controls/ThumbnailGrid.axaml`** — `ItemsRepeater` with `UniformGridLayout`. Each cell: `Image` bound to `ThumbnailPath`, 256×256, `Stretch.UniformToFill`. Video and audio items show a media-type overlay icon (simple visual distinction, no playback in MVP).

- [ ] **App wiring**: `Program.cs` builds `IHost` with all services registered. DI in `App.axaml.cs`.

---

## Phase 8 — CLIP Python Service + C# Adapter Plugin

**Goal:** `plugins/clip-embedder/` is a FastAPI service computing 512-dim CLIP embeddings for images. A C# adapter wraps it as `IInProcessPlugin`. Core sees only `IInProcessPlugin`.

**Done when:** Python service running, adapter registered in DI, pipeline processes one image job, `/debug/files/{id}` shows an embedding row. Audio and video-only files are correctly skipped (no embedding job enqueued).

### Tasks

- [ ] **`src/PiKoRe.Plugins.ClipAdapter/ClipAdapterPlugin.cs`** — implements `IInProcessPlugin`:
  - `CapabilitiesProduced`: `[Capabilities.Embedding]`
  - `RequiredCapabilities`: `[Capabilities.Thumbnail]`
  - `SupportedMediaTypes`: `[MediaTypes.Image]` — CLIP is a visual model; video frames not supported in MVP
  - Reads endpoint from `IConfiguration["Plugins:ClipEmbedder:Endpoint"]` (default `http://localhost:5001`)
  - Calls `POST /analyze` on the Python service via `IHttpClientFactory` with Polly retry
  - Writes embedding to PostgreSQL via `IMediaStore`

- [ ] **`plugins/clip-embedder/plugin.json`**:
  ```json
  {
    "name": "clip-embedder",
    "version": "1.0.0",
    "capabilities_produced": ["embedding"],
    "requires_capabilities": ["thumbnail"],
    "supported_media_types": ["image/*"],
    "endpoint": "http://localhost:5001",
    "gpu_memory_mb": 800,
    "startup_command": null
  }
  ```

- [ ] **`plugins/clip-embedder/plugin.py`** — FastAPI app:
  - `GET /health` — `{"status":"ok"}`
  - `POST /analyze` — receives `AnalysisRequest` JSON, loads image from `preview_path`, runs CLIP, returns `float[]`
  - `POST /embed-text` — encodes a text query string with CLIP, returns `float[]` (needed by Phase 9 search)
  - On startup: `POST http://localhost:7700/api/plugins/register` with manifest (retry with backoff)
  - Structured logging with `job_id`, `file_id`

- [ ] **`plugins/clip-embedder/requirements.txt`** and **`install.sh`** as originally planned.

- [ ] **Enable `"embedding"` in `pipeline_config`** — document in the quickstart that the user must add `"embedding"` to the enabled list after installing the CLIP plugin (see D-023).

---

## Phase 9 — Text Search

**Goal:** Search bar sends a CLIP text query; backend embeds it and returns top-20 visually matching images via pgvector ANN.

**Note:** Embedding-based search returns **image results only** in MVP — only images have CLIP embeddings. Audio and video files appear in the grid but not in embedding search results. A future phase may add audio transcription search and video keyframe embedding.

**Done when:** Typing "sunset beach" returns relevant photos within 1 second.

### Tasks

- [ ] **`IMediaStore.SearchByEmbeddingAsync`** — implement in `PostgresMediaStore.cs` (returns `IReadOnlyList<Guid>` — file IDs only, see D-027):
  ```sql
  SELECT file_id FROM embeddings ORDER BY vector <=> @queryVector LIMIT @limit
  ```

- [ ] **Text-to-embedding**: call `POST /embed-text` on the CLIP plugin via `IHttpClientFactory`. Add endpoint to Phase 8 plugin (same model, text encoder path).

- [ ] **`MainViewModel.SearchCommand`** — flow:
  1. Call CLIP `POST /embed-text` → `float[]` query vector
  2. Call `IMediaStore.SearchByEmbeddingAsync(vector, 20)` → `IReadOnlyList<Guid>` file IDs
  3. For each ID call `IFileIndexStore.GetByIdAsync` → `IndexedFile` (to get path + media_type for ThumbnailItem)
  4. Update `ObservableCollection<ThumbnailItem>`

- [ ] **Search bar in UI**: `TextBox` + search button in top bar. Bound to `SearchCommand`. Results show only items with embeddings; if no results, display "No matching images found" (not an error).

---

## Phase 10 — MVP Polish + Integration Test

**Goal:** End-to-end: configure folder → scan → thumbnails appear → CLIP runs → text search returns results. All phases working together across image, video, and audio files.

**Done when:** Fresh machine setup achieves the MVP steps in `docs/architecture.md §MVP`.

### Tasks

- [ ] **`appsettings.json`** with sane defaults: DB connection strings, port numbers, library paths placeholder, `Pipeline:MaxCpuSlots`, `Pipeline:EnabledCapabilities` (initial value: `["exif","thumbnail"]`).
- [ ] **README.md** quick-start: prerequisites, `docker compose up -d`, `install.sh` for each plugin, `dotnet run`.
- [ ] **Logging verification**: confirm Seq at `:8081` receives structured events with `file_id`, `plugin_name`, `job_id`, `media_type` context.
- [ ] **Media type coverage test**: scan a folder with 1 JPEG + 1 MP4 + 1 MP3. Assert:
  - JPEG: exif ✓, thumbnail ✓, embedding ✓ (after CLIP installed)
  - MP4: exif ✓, thumbnail ✓, embedding ✗ (not queued — CLIP is image-only)
  - MP3: exif ✓, thumbnail ✗ (not queued — ThumbnailPlugin declares `[image/*, video/*]`), embedding ✗
- [ ] **Error recovery test**: kill CLIP plugin mid-job. Verify job marked `Failed`. Retry via `/debug/jobs/retry`. No crash in core.
- [ ] **Idempotency test**: scan same folder twice. No duplicate `file_index` rows, no duplicate jobs.
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
- **D-021**: Core never makes outbound HTTP calls for plugin dispatch. External services are wrapped by a C# adapter plugin.
- **D-022**: `IExternalPlugin` interface does not exist. Use `ExternalPluginInfo` record.
- **D-023**: `pipeline_config.dag_json` is a flat JSON array of enabled capability names. Graph topology comes from plugin declarations. New plugins are not auto-added.
- **D-024**: `IPlugin.SupportedMediaTypes` is the single source of truth for file-type filtering. Use `MediaTypes.IsSupported(fileMediaType, plugin.SupportedMediaTypes)`. Never filter by file type inside plugin logic — return a clean `AnalysisResult` or let the DagEngine skip the job before it is even enqueued.
- **D-025**: Audio files are first-class citizens. Scanner supports `.mp3`, `.flac`, `.wav`, `.aac`, `.m4a`, `.ogg`, `.opus`, `.wma`. Audio-specific analysis capabilities (transcription, fingerprinting) are post-MVP.
- **D-026**: `JobResult` carries `FileId`, `Capability`, and `MediaType`. `MediaType` is included so `DagEngine` can check plugin compatibility without a second DB query — the event fires while the job row is still `running` (before `PipelineWorker` calls `MarkCompletedAsync`), so querying the DB for media type would require an extra `IMediaStore` or file-index query. `LocalSequentialRunner` populates all three from the `Job` it just executed.
- **D-027**: `IMediaStore` is PostgreSQL-only. `GetByIdAsync` removed; replaced with `GetFileDetailsAsync → FileAnalysisDetails?`. `SearchByEmbeddingAsync` returns `IReadOnlyList<Guid>` (file IDs only). `IndexedFile` must never appear on a PostgreSQL interface. The debug endpoint `/debug/files/{id}` calls both `IFileIndexStore.GetByIdAsync` (SQLite, for identity) and `IMediaStore.GetFileDetailsAsync` (PostgreSQL, for analysis data) and stitches them into a response DTO.
- **D-028**: `IFileIndexStore` (Core) + `SqliteFileIndexStore` (Data) own all SQLite `file_index` operations. Methods: `GetByPathAsync`, `GetByIdAsync`, `UpsertAsync`. `FileScanner` depends on `IFileIndexStore`, not `IJobQueue`. Defined in Phase 5 pre-req.
- **Job constructor note**: When constructing `Job` objects to pass to `EnqueueAsync`, always pass `null` for `FilePath` and `null` for `MediaType`. These are transient fields populated at dequeue time via JOIN with `file_index`; they are never written to `job_queue`.
