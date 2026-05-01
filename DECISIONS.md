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

---

*To override a decision: add a new row with the new decision and add "Overrides D-XXX" in the Reason column.*
