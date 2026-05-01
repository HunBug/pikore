# PiKoRe — Architecture Design v3
*Extensible local photo/video analysis framework*
*One-man project. Simplicity first. Space for growth.*

---

## ⚠️ Document Status: Kickoff Design

This document was produced in the initial architecture kickoff session. It represents the best thinking available at that point in time — not a frozen specification.

**Every decision in this document can and should be overridden** if there is a concrete reason to do so. The goal of this document is to give future development sessions (human or agentic) a coherent starting point and the reasoning behind early choices — not to constrain future work.

When overriding a decision: add a new row to `DECISIONS.md` with the updated decision, what it replaces, and why. Do not delete old entries — the history of reasoning is valuable.

If you are an AI agent reading this: treat this as context and intent, not as immutable requirements. Ask the user if something seems wrong or outdated.

---

## Refined Core Thesis

A thin C# shell that owns the UI, the plugin lifecycle, and the pipeline engine. Almost everything else — including data backends, analysis workers, and even the HTTP API — is a plugin. The core is minimal on purpose. Complexity lives in plugins, not in the core.

**Key principle:** The web API exposer is itself a plugin. The core has no opinion about HTTP access. If you want REST endpoints, install the API plugin. This keeps the core clean and avoids building infrastructure nobody asked for yet.

---

## Database Strategy — Two-tier

### Tier 1: Core SQLite (framework-internal only)
SQLite handles only framework concerns. It never stores actual analysis data.

```
plugin_registry   (id, name, version, endpoint, capabilities, status, config_json)
job_queue         (id, file_id, capability, status, plugin_id, priority, created, updated, error)
pipeline_config   (dag definition, capability dependencies)
file_index        (id, path, size, mtime, hash, ingested_at)
system_config     (key, value)
```

WAL mode enabled from day one. Required for concurrent readers + writer.

### Tier 2: PostgreSQL + pgvector (real data)
All analysis results live here.

```
thumbnails        (file_id, size_class, data_path)
metadata          (file_id, key, value, source_plugin)
tags              (file_id, label, confidence, source_plugin)
embeddings        (file_id, model_id, vector vector(512))
faces             (id, file_id, bbox_json, embedding vector(512))
persons           (id, name, cover_face_id)
face_person       (face_id, person_id, confidence)
scores            (file_id, key, value, source_plugin)
descriptions      (file_id, text, source_plugin)
```

`SELECT file_id FROM embeddings ORDER BY vector <=> $query_vector LIMIT 20` — CLIP text search in one line.

**Migrations:** DbUp. Numbered `.sql` files in `src/PiKoRe.Data/Migrations/`. Never ALTER TABLE manually.

---

## Plugin System — Language Agnostic by Design

### The core knows nothing about plugin internals

The plugin system defines a protocol, not a runtime. The core does not know or care:
- What language a plugin is written in
- How the plugin manages its dependencies
- Whether it uses a venv, conda, Docker, a compiled binary, or anything else
- How it loads models
- Whether it is one process or ten

The only contract between core and plugin:

1. Plugin registers at startup via HTTP POST to core's registration endpoint
2. Core calls the plugin via HTTP POST with a JSON payload
3. Plugin returns a JSON result

Everything else is the plugin's private business.

### Plugin manifest (plugin.json)

```json
{
  "name": "face-detector",
  "version": "1.0.0",
  "capabilities_produced": ["faces"],
  "requires_capabilities": ["thumbnail"],
  "endpoint": "http://localhost:5001",
  "gpu_memory_mb": 1200,
  "startup_command": null
}
```

`startup_command` is an opaque shell string. `python plugin.py`, `./plugin`, `docker run my-plugin`, `node plugin.js` — all equally valid. If null, the plugin is expected to already be running.

### No common runtime, no shared venv

There is no shared Python environment. Each plugin is entirely self-contained. If a plugin needs Python with specific packages, that is a plugin installation concern, not a core concern.

Convention (suggested, not enforced):
- Each plugin lives in `plugins/{name}/`
- Ships `requirements.txt` and optionally `install.sh`
- Manages its own virtualenv
- Starts its own server on a configured port

---

## Pipeline Runner — Simple First, Extensible by Interface

```csharp
public interface IJobRunner
{
    Task<JobResult> RunAsync(Job job, CancellationToken ct);
}
```

### Default: LocalSequentialRunner

One GPU job at a time (semaphore), N CPU jobs in parallel. No Docker, no remote. Build this first, run it for months.

```csharp
public class LocalSequentialRunner : IJobRunner
{
    private readonly SemaphoreSlim _gpuSlot = new(1, 1);
    private readonly SemaphoreSlim _cpuSlots = new(4, 4);

    public async Task<JobResult> RunAsync(Job job, CancellationToken ct)
    {
        var slot = job.Plugin.RequiresGpu ? _gpuSlot : _cpuSlots;
        await slot.WaitAsync(ct);
        try { return await CallPlugin(job, ct); }
        finally { slot.Release(); }
    }
}
```

### Future runners (don't build — the interface is the extensibility point)
- `DockerRunner` — per-job container isolation
- `RemoteNodeRunner` — jobs dispatched to another machine on LAN
- `MultiRunnerRouter` — routes by resource type

### On job queue complexity
v1: SQLite job table + polling loop. Migrate to Hangfire when the SQLite approach is demonstrably the bottleneck. The `IJobRunner` interface makes it a swap.

---

## Observability — Always Active, Always Structured

In a plugin-heavy pipeline system, observability is not optional. Designed in from day one.

### Structured Logging — Serilog

Every log entry carries context: `file_id`, `plugin_name`, `job_id`, `capability`.

```csharp
Log.ForContext("plugin", job.Plugin.Name)
   .ForContext("file_id", job.FileId)
   .Information("Job started");
```

Active sinks (configured, not hardcoded):
- `Serilog.Sinks.Console` — always on
- `Serilog.Sinks.File` — rolling daily files
- `Serilog.Sinks.Seq` — Seq log viewer at http://localhost:8081
- `Serilog.Sinks.Grafana.Loki` — for Grafana integration when needed

### Traces & Metrics — OpenTelemetry

```csharp
services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource("PiKoRe.Pipeline")
        .AddSource("PiKoRe.PluginHost")
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddMeter("PiKoRe.Pipeline")
        .AddPrometheusExporter());
```

Key metrics: `jobs_queued`, `jobs_completed`, `jobs_failed` (per plugin/capability), `plugin_latency_ms` histogram, `gpu_slot_wait_ms`, `files_indexed_total`.

### Debug HTTP Endpoint — always active, localhost:7701

```
GET  /debug/plugins          — registered plugins + status
GET  /debug/jobs/running     — currently executing
GET  /debug/jobs/queued      — pending queue depth per capability
GET  /debug/jobs/failed      — recent failures with error details
GET  /debug/pipeline/dag     — current DAG as JSON
GET  /debug/gpu              — GPU slot status
GET  /debug/files/{id}       — all analysis results for one file
POST /debug/jobs/{id}/retry  — manually retry a failed job
POST /debug/plugins/{name}/ping — force health check
```

This is built immediately after the core pipeline. It works with a browser or curl. It is the fastest way to answer "what is the system doing right now."

### Plugin observability
Plugins write to stdout/stderr. Core captures and forwards to its log pipeline. Plugins should include `job_id` and `file_id` in all log entries. Optional progress reporting:

```
POST http://localhost:7700/api/jobs/{job_id}/progress
{ "percent": 42, "message": "Processing frame 420/1000" }
```

---

## What To Steal (and what not to)

| From | Steal | Don't steal |
|---|---|---|
| **VSCode** | plugin manifest format, capability-based routing, process boundary isolation | the extreme IPC complexity |
| **Ollama** | simple REST design, model-as-resource, pull-to-install UX | reinventing it — *use* Ollama for VLM plugins |
| **Home Assistant** | plugin config entries (each plugin owns its config blob) | YAML-config-everything |
| **Immich** | external library scanning (read-only), job status visibility, re-run UI | monolithic ML container |
| **Hangfire** | dashboard concept, retry/backfill thinking | pulling it in before needed |
| **OpenTelemetry** | the entire instrumentation model | rolling custom metrics/tracing |

---

## Common Pitfalls and Anti-Patterns

### Architecture

**The framework trap.** Building the plugin framework instead of the photo tool. Fix: write two concrete plugins before defining the interface.

**Premature generalization.** Generalize when you have two concrete examples. Not before.

**EAV schema abuse.** `(file_id, key, value)` is correct for open-ended extension. It is wrong for columns you query and filter on. When you find yourself doing `WHERE key = 'foo'` frequently, make it a real column.

**Schema migration neglect.** DbUp, numbered files, git. Non-negotiable.

**Ignoring WAL mode.** Enable it at SQLite creation. Multiple processes + no WAL = locking errors.

**Designing plugin protocol for the first plugin.** Expect one redesign after the second and third plugin reveal the gaps.

### One-man project

**"I'll remember why."** You won't. Write it in DECISIONS.md at decision time.

**Session context loss.** CURRENT_STATE.md. Update at end of every session.

**One more feature before MVP.** MVP = useful to you personally. Ship it. Then add features.

**Yak shaving.** If the task you're doing is not the task you started with, stop.

**No tests → ossified bad decisions.** Test the pipeline engine, plugin protocol, and DB queries at minimum.

### Proven failures

**Custom message queue before proving the need.** SQLite polling is boring and sufficient. Add complexity only when the simple approach is demonstrably the bottleneck.

**gRPC for plugin communication.** Protobuf schema management adds friction with no benefit at local call rates.

**ORM for complex analytical queries.** EF Core for CRUD, Dapper for complex reads.

**Building UI before the pipeline is proven.** Pipeline first. Verify with raw SQL. Then add UI.

---

## Technology Inventory

### C# / .NET

| Package | Purpose |
|---|---|
| `Avalonia` | Cross-platform XAML UI |
| `ImageSharp` | Managed image processing |
| `FFMpegCore` | Video thumbnail/frame extraction |
| `MetadataExtractor` | EXIF/GPS/IPTC extraction |
| `Microsoft.ML.OnnxRuntime` + `.Gpu` | ONNX model inference + CUDA |
| `YoloDotNet` | YOLO object detection in C# |
| `FaceONNX` | Face pipeline in C# |
| `Npgsql` | PostgreSQL driver |
| `Dapper` | Complex reads (raw SQL + mapping) |
| `Microsoft.EntityFrameworkCore` | Schema management + CRUD |
| `Microsoft.Data.Sqlite` | Framework-internal SQLite |
| `DbUp` | SQL migration runner |
| `Microsoft.Extensions.Hosting` | Generic host, background services |
| `MediatR` | In-process event bus |
| `Polly` | Resilience: retries, circuit breakers |
| `System.Threading.Channels` | Producer/consumer queues |
| `Serilog` + sinks | Structured logging |
| `OpenTelemetry` | Traces + metrics |
| `xUnit` + `Testcontainers.PostgreSql` | Testing |
| `NSubstitute` | Mocking |

### Python (plugin-side)

| Package | Purpose |
|---|---|
| `FastAPI` + `uvicorn` | Plugin HTTP server |
| `transformers` | HuggingFace model loading |
| `sentence-transformers` | CLIP embeddings |
| `insightface` | Face detection + recognition |
| `NudeNet` | NSFW detection |
| `onnxruntime-gpu` | ONNX inference |
| `Pillow`, `numpy` | Image processing, tensors |
| `pgvector`, `asyncpg` | Write results to PostgreSQL |

### Docker Services

| Image | Purpose |
|---|---|
| `pgvector/pgvector:pg16` | PostgreSQL + pgvector |
| `datalust/seq` | Structured log viewer (dev) |
| `grafana/grafana` + loki + tempo + prometheus | Full observability stack (optional) |
| `ollama/ollama` | Local LLM/VLM serving |
| `ghcr.io/huggingface/text-embeddings-inference` | Optimized embedding model serving |

---

## Core Boundary

```
CORE (C#, minimal, non-swappable):
  ├── UI shell + virtual grid (Avalonia)
  ├── Plugin registry + lifecycle
  ├── Analysis Protocol contracts (interfaces only)
  ├── MediatR event bus
  ├── Job queue + DAG engine (SQLite, IJobRunner interface)
  ├── LocalSequentialRunner
  ├── File scanner + watcher
  ├── SQLite (framework tier)
  ├── PostgreSQL + DbUp migrations
  ├── IMediaStore + default Postgres implementation
  ├── Serilog + OpenTelemetry (always on)
  └── Debug HTTP endpoint (localhost:7701, always on)

DEFAULT PLUGINS:
  ├── ExifExtractor          C# in-process   MetadataExtractor
  ├── ThumbnailGenerator     C# in-process   ImageSharp + FFMpegCore
  ├── ClipEmbedder           Python external  sentence-transformers
  ├── FaceDetector           Python external  insightface
  ├── FaceClusterer          C# in-process   HNSW + DBSCAN
  ├── ObjectDetector         C# in-process   YoloDotNet
  ├── NsfwScorer             Python external  NudeNet
  └── VlmCaptioner           Python external  → Ollama

OPTIONAL PLUGINS:
  ├── ApiExposer             C# AspNetCore
  ├── AestheticScorer        Python external
  ├── DuplicateFinder        C# in-process
  ├── RemoteNodeRunner       C# IJobRunner
  └── MapView / TimelineView C# Avalonia panels
```

---

## MVP

1. Configure a folder path
2. App scans and indexes into SQLite
3. Thumbnails in virtual scrolling grid
4. One Python plugin (CLIP) runs on each file
5. Text search works
6. Debug endpoint at :7701 shows live state

**~5–7 weeks of focused sessions.**

---

## Guide for Agentic Coders

*Read before generating any code.*

### Context
Personal, long-running, one-man project. Owner (Boo Kumpli) prioritizes understanding over volume. Work in small, verifiable increments. Do not generate ahead of understanding.

This is a kickoff design. All decisions are overridable. If something seems wrong, say so rather than silently implementing something inconsistent with stated intent.

### Before starting any session
1. Read `CURRENT_STATE.md` — where things are right now
2. Read `DECISIONS.md` — do not re-litigate settled decisions without cause
3. Read `DISCOVERIES.md` — prior sessions may have documented codebase findings
4. Confirm you are on the correct branch

### Technology rules
- Target **.NET 9** (or latest stable). No .NET Framework or .NET Core 3.1 era patterns.
- **Avalonia** for UI. Not WPF, not MAUI.
- **Serilog** for logging. No `Console.WriteLine` in production code paths.
- **OpenTelemetry** for traces and metrics. Use `ActivitySource` and `Meter`.
- Prefer **production-proven NuGet packages** with clear ownership and active maintenance. Check NuGet download counts and last publish date. Flag uncertain packages rather than silently introducing them.
- **C# 13 language features** are welcome where they reduce noise. Not to be clever — to be clear.

### Code generation rules
- **One thing at a time.** Interface first, confirm, then implementation.
- **Tests are not optional.** Any non-trivial logic needs at least one test. Use `xUnit`. Use `Testcontainers.PostgreSql` for DB-touching tests.
- **No magic strings.** Capability names, config keys, manifest fields — define as constants or enums.
- **Async all the way down.** No `.Result` or `.Wait()` outside entry points.
- **`IHttpClientFactory`, not `new HttpClient()`.**
- **`CancellationToken` threaded through** every async method touching I/O. Non-negotiable.
- **No silent exception swallowing.** All exceptions must be logged at minimum.

### Project structure
```
src/PiKoRe.Core/           interfaces, event bus, job queue, file scanner
src/PiKoRe.UI/             Avalonia application
src/PiKoRe.Data/           DB layer, migrations in Migrations/
src/PiKoRe.Plugins.*/      in-process plugins
tests/
plugins/{name}/            external plugins (Python etc.)
docs/
docker-compose.yml
```

Migration files: `src/PiKoRe.Data/Migrations/0001_initial_schema.sql` etc.

### Documentation
- New interfaces get XML doc comments explaining what they *represent*, not just what the methods do.
- Non-obvious decisions in generated code get inline comments with the reason.
- Architectural decisions go into `DECISIONS.md`.
- Update `CURRENT_STATE.md` at the end of every session.

### Red lines
- Do not write to files outside configured library paths without explicit permission.
- Do not introduce network calls to external services without flagging it. This is a local-first, privacy-first project.
- Do not generate "working prototypes" that skip error handling, logging, or cancellation support. These are not nice-to-haves in a pipeline system.
- Do not silently change the plugin protocol contract. It is a shared boundary. Discuss first.

---

*v3 — PiKoRe rename, plugin language-agnosticism, no shared venv, observability as first-class, agentic coder guide, debug endpoint always active.*
