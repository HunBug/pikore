# PiKoRe — Claude Code Instructions

## Start here

Read these files before doing anything else. They are the source of truth.

```
CURRENT_STATE.md                        ← where the project is right now, next step
DECISIONS.md                            ← settled decisions, append-only
DISCOVERIES.md                          ← codebase findings from prior sessions
.github/copilot-instructions.md         ← coding rules, tech choices, red lines
docs/architecture.md                    ← full architecture rationale
```

All coding rules, technology choices, structural conventions, and red lines are in `.github/copilot-instructions.md`. They apply equally here. Do not skip it.

---

## Claude Code-specific guidance

### Work style
- Work in small, verifiable increments. Do not generate an entire subsystem unprompted.
- After completing a task, always update `CURRENT_STATE.md`.
- Significant architectural decisions go into `DECISIONS.md` (append only, never delete).

### Running commands
- `docker compose up -d` — safe, starts the dev stack (postgres, seq, ollama)
- `dotnet build` / `dotnet test` — safe to run freely
- `dotnet run` — ask before running if the project has side effects (file scanning, DB writes)
- Never run commands that write outside the project directory or configured library paths
- Never install system packages without asking first

### File operations
- Never modify files under the configured media library paths (NAS mounts, external drives)
- Migration files in `src/PiKoRe.Data/Migrations/` are append-only — never edit existing ones
- Always create migration files with the correct next sequence number

### When something looks wrong
If anything in the architecture docs seems outdated, inconsistent, or wrong — say so before implementing. Do not silently work around it.
