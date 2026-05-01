# PiKoRe — GitHub Copilot Instructions

## What this project is

PiKoRe is a local photo/video intelligence framework. Thin C# core (UI, plugin engine, pipeline), everything else is a plugin. Privacy-first, nothing leaves the machine. One-man long-running project. Simplicity over cleverness.

## Before starting any task

Always read these files first — they tell you where the project is right now:
- `CURRENT_STATE.md` — current phase, next step, open blockers
- `DECISIONS.md` — settled architectural decisions, do not re-litigate without cause
- `DISCOVERIES.md` — codebase findings from prior sessions
- `docs/architecture.md` — full architecture rationale and kickoff decisions

## Core technology choices (don't suggest alternatives without flagging)

- **UI:** Avalonia only. Not WPF, not MAUI.
- **Logging:** Serilog only. Never `Console.WriteLine` in production code paths.
- **Observability:** OpenTelemetry for traces and metrics (`ActivitySource`, `Meter`).
- **Database:** SQLite (framework internals only) + PostgreSQL + pgvector (all analysis data).
- **Migrations:** DbUp with numbered `.sql` files in `src/PiKoRe.Data/Migrations/`. Never `ALTER TABLE` manually.
- **In-process events:** MediatR.
- **Resilience:** Polly on all external calls (plugin HTTP, DB).
- **Target:** .NET 9+. No legacy .NET Framework patterns.

## Non-negotiable code rules

- Every async method touching I/O receives and threads a `CancellationToken`. No exceptions.
- Use `IHttpClientFactory`, never `new HttpClient()`.
- No silent exception swallowing. All caught exceptions must be logged at minimum.
- No magic strings. Capability names, config keys, manifest fields → constants or enums.
- Structured log entries carry context: `file_id`, `plugin_name`, `job_id`. Use `Log.ForContext(...)`.
- `async` all the way down. No `.Result` or `.Wait()` outside entry points.

## Project structure

```
src/PiKoRe.Core/           interfaces, event bus, job queue, file scanner
src/PiKoRe.UI/             Avalonia app
src/PiKoRe.Data/           DB layer + Migrations/
src/PiKoRe.Plugins.*/      in-process C# plugins
tests/
plugins/{name}/            external plugins (Python, etc.) — self-contained, own venv
docs/
docker-compose.yml         postgres+pgvector, Seq, Ollama
```

## Plugin system — critical boundary

The core knows nothing about plugin internals. The protocol is HTTP JSON only. The core does not manage Python, venvs, or model loading. A plugin manifest (`plugin.json`) declares capabilities and an optional opaque `startup_command` string — core executes it as-is without interpretation. Do not add runtime-specific logic to core.

## What to do when uncertain

- Check `DECISIONS.md` before making an architectural choice.
- If a decision seems wrong or outdated, say so explicitly — do not silently implement something inconsistent with stated intent.
- If introducing a new NuGet package: verify it has active maintenance and meaningful adoption. Flag uncertain packages rather than silently adding them.
- Prefer production-proven libraries over small/single-maintainer ones for critical path components.

## After completing a task

Update `CURRENT_STATE.md` with what was done, what the next step is, and any open questions. Add any significant architectural decision made during implementation to `DECISIONS.md`.
