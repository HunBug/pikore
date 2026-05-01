# PiKoRe — Current State

*Update this file at the end of every development session. It is the first thing to read at the start of a new session.*

---

## Phase
**Architecture complete. Implementation not started.**

---

## Last session
Kickoff architecture meeting. Produced `docs/architecture.md`. Named the project PiKoRe. Initialized repository.

---

## Next concrete step
Create the .NET solution skeleton:

```
src/
  PiKoRe.Core/          ← core library (interfaces, event bus, job queue, file scanner)
  PiKoRe.UI/            ← Avalonia application
  PiKoRe.Data/          ← DB layer (SQLite + PostgreSQL), migrations
  PiKoRe.Plugins.Exif/  ← first in-process plugin
tests/
  PiKoRe.Core.Tests/
  PiKoRe.Data.Tests/
plugins/                ← external (Python etc.) plugins live here
docs/
docker-compose.yml      ← postgres + seq
```

Run: `dotnet new sln -n PiKoRe` and create the projects above.

---

## Open questions / blockers
- None at this stage.

---

## Recent decisions
See `DECISIONS.md` for the full log. No implementation decisions made yet — all current decisions are architectural.

---

## Known issues / tech debt
- None yet.
