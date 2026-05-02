# PiKoRe — Decisions Log

*Append only. Never delete entries. When overriding a decision, add a new row referencing the old one.*  
*Format: ID | Decision | Alternatives considered | Reason | Date | Overrides*

---

| ID | Decision | Alternatives considered | Reason | Date |
|---|---|---|---|---|
| D-001 | SQLite for framework internals (job queue, plugin registry, file index). PostgreSQL + pgvector for all analysis data. | SQLite for everything | sqlite-vec insufficient for 500K × 512-dim ANN at query time; pgvector solves cleanly. SQLite remains for operational/framework data where zero-setup matters. | 2026-05 |
| D-002 | HTTP JSON as the plugin protocol. Plugins register with core via POST, core calls plugins via POST. | gRPC, named pipes, shared memory | Language-agnostic. Debuggable with curl. No protobuf schema management. Sufficient for local call rates. | 2026-05 |
| D-003 | API-exposer (REST access to the library) is an optional plugin, not part of core. | Core hosts REST API | Keeps core minimal. Not all deployments need external access. The plugin installs when needed. | 2026-05 |
| D-004 | `IJobRunner` interface in core. Default implementation: `LocalSequentialRunner` (1 GPU slot, N CPU slots). | Hangfire from day one | Avoid operational complexity before it is demonstrated as needed. The interface makes a later migration a swap, not a rewrite. | 2026-05 |
| D-005 | DbUp for schema migrations. Plain numbered `.sql` files. | EF Core Migrations | DbUp is not tied to any ORM or project type. SQL files are readable, reviewable, and always correct. | 2026-05 |
| D-006 | MediatR for in-process event bus. | Custom event system, direct callbacks | Well-understood pattern. Eliminates coupling between pipeline components. Easy to test. | 2026-05 |
| D-007 | Use Ollama for VLM/LLM plugins (moondream, llava, etc.). Plugins call Ollama's OpenAI-compatible API. | Custom Python FastAPI model server per model | Ollama handles model management, GPU memory, hot-swap, and multi-model. Eliminates an entire category of plugin infrastructure. | 2026-05 |
| D-008 | Plugin system has no opinion on plugin runtime, language, or dependency management. No shared venv. Startup command in manifest is an opaque shell string. | Managed Python venv, enforced Docker | Correct separation of concerns. Core owns the protocol, not the runtime. Plugins are self-contained. | 2026-05 |
| D-009 | Serilog + OpenTelemetry as observability infrastructure, built into core from day one. Debug HTTP endpoint always active on localhost:7701. | Add logging later; rely on console output | Impossible to retrofit cleanly into a pipeline/plugin system. Structured logs and the debug endpoint are the primary debugging tools for this architecture. | 2026-05 |
| D-010 | Project named PiKoRe (Picture · Kollection · Recognition). Kawaii spelling intentional. | PixCore | Kawaii. 🌸 | 2026-05 |
| D-011 | SQLite and app data stored in a configured location (configurable, hardcoded default acceptable for v1). Not tied to binary path. | Fixed path next to binary | Configurable from day one; even a hardcoded default keeps the door open without coupling to deployment layout. | 2026-05 |
| D-012 | Core HTTP split by purpose: `:7700` = plugin registration + job progress API (plugins call in); `:7701` = debug/admin endpoint (human-facing, browser/curl). | Single port for both | Separates the plugin-facing contract from the debug surface. Debug port can be restricted or disabled independently. | 2026-05 |
| D-013 | Core does not execute `startup_command` in v1. Plugins must be pre-started manually before core runs. | Core orchestrates plugin startup | Eliminates process lifecycle complexity from MVP. The manifest field exists for a future auto-start feature without requiring a protocol change. | 2026-05 |
| D-014 | In-process plugins implement `IInProcessPlugin` and are called directly in the same process — no HTTP round-trip. External plugins implement `IExternalPlugin` and are called via HTTP. Both share capability names via `Capabilities` constants. | All plugins go through HTTP | Avoids serialisation overhead and localhost network call for C# plugins. The pipeline engine handles both paths transparently. | 2026-05 |
| D-015 | Drop EF Core entirely. Use Dapper + DbUp for PostgreSQL; `Microsoft.Data.Sqlite` for SQLite. No ORM in the stack. | EF Core for CRUD + DbUp for migrations | EF Core and DbUp conflict as dual migration owners. Core queries involve pgvector ANN (`<=>` operator) which EF Core cannot express in LINQ — raw SQL is required anyway. Dapper is a thin mapper with no overhead. | 2026-05 |
| D-016 | Avalonia single-window layout: virtual thumbnail grid as main area, left navigation sidebar (All / By Date / By Tag / Settings), top search bar. | Multi-window | Minimal surface for MVP. Sidebar slots exist structurally but need no content until later features are built. | 2026-05 |
| D-017 | Target `net10.0` (SDK 10.0.104 LTS) for all projects instead of `net9.0`. | net9.0 as originally planned | User preference: always use the current LTS release. .NET 9 is STS, .NET 10 is LTS. | 2026-05 |
| D-018 | `Polly.Extensions.Http` omitted from `PiKoRe.Core`. Use base `Polly` (8.6.6) + `Microsoft.Extensions.Http.Polly` when HTTP client resilience is needed in Phase 4 (`PiKoRe.Host`). | Include Polly.Extensions.Http | Package is deprecated in Polly v8+; it conflicts with the current Polly API surface. Add `Microsoft.Extensions.Http.Polly` to the host project only, not core. | 2026-05 |
| D-019 | Solution file is `PiKoRe.slnx` (new XML format). The .NET 10 SDK defaults to `.slnx` instead of the legacy `.sln` format. | Classic .sln | `.slnx` is the supported new format. It is handled transparently by `dotnet` CLI and modern IDEs. No action needed. | 2026-05 |
| D-020 | SQL migration files are compiled as embedded resources inside `PiKoRe.Data.dll` (via `<EmbeddedResource Include="Migrations/**/*.sql" />`). DbUp loads them with `WithScriptsEmbeddedInAssembly`. | Load from filesystem at runtime | Embedded resources work regardless of working directory or deployment layout. Filesystem approach would require knowing the path to the build output or install location, which varies by environment. | 2026-05 |

---

*To override a decision: add a new row with the new decision and add "Overrides D-XXX" in the Reason column.*
