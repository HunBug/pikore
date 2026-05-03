# PiKoRe Learning Curriculum

*Planned lesson list. Update this file when lessons are written (change status to `done`).*
*When adding a new unplanned lesson, add a row here with status `done` and the git hash.*

---

## Structure philosophy

Lessons are **problem-driven episodes**, not topic-by-topic chapters.

Each episode follows this arc:
1. **You encounter something in the code** — a pattern, a keyword, a decision that isn't obvious
2. **The naive attempt** — what you'd write without this knowledge, and what breaks
3. **What the codebase actually does** — the real code, explained step by step
4. **The general principle** — now that you've seen it in context, the abstract idea makes sense
5. **Back to practice** — another real use of the same idea in the repo, or a variation

Tracks are *tags*, not organizational structure. They're recorded in each lesson for searchability, but the curriculum flows as a single sequence.

| Track tag | What it means |
|-----------|---------------|
| `csharp` | C# language feature — syntax, type system, runtime behaviour |
| `pattern` | Design pattern — a named solution to a recurring problem |
| `arch` | Architectural decision — why the system is built this way |
| `repo` | End-to-end walkthrough — how data flows through the actual system |

---

## Diagrams

**Simple inline diagrams:** Use Mermaid fenced code blocks (` ```mermaid `). Renders in VSCode preview + GitHub. No build step.

**Complex diagrams:** Write a Python script in `learning/tools/` that generates PNG files into `learning/assets/`. The script is the diagram source — commit both. Setup and run:

```sh
cd learning/tools
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
cd ../..
python learning/tools/generate_diagrams.py
```

Also requires the `graphviz` system binary: `sudo pacman -S graphviz` / `sudo apt install graphviz` / `brew install graphviz`.

Reference a generated image in a lesson with: `![Description](../assets/diagram_name.png)`

---

## Lesson sequence

### Block A — Opening the codebase (can be written at git `d96de42b`)

*The reader opens the repo for the first time. These lessons cover what they'll see first and be confused by.*

| # | Episode opening | Tracks | Status | Key files |
|---|-----------------|--------|--------|-----------|
| L001 | You see `IPlugin`, `IJobRunner`, `IJobQueue` everywhere. What is an `interface` and why does every important thing in this repo have one? | csharp, arch | done @ `a96b94c` | `Core/Abstractions/IPlugin.cs`, `IJobRunner.cs`, `IJobQueue.cs` |
| L002 | You look at `Job`, `JobResult`, `IndexedFile`. They're defined with `record`, not `class`. What's the difference, and why does it matter for a pipeline? | csharp | done @ `a96b94c` | `Core/Models/Job.cs`, `JobResult.cs`, `IndexedFile.cs` |
| L003 | Every single method ends with `CancellationToken ct`. What is this? What happens if you ignore it? | csharp | done @ `a96b94c` | `Data/SqliteJobQueue.cs`, `Core/Pipeline/LocalSequentialRunner.cs` |
| L004 | Some fields are `string?` or `Guid?` — the question mark means something. Others crash if they're null. How does C# track this? | csharp | done @ `a96b94c` | `Core/Models/Job.cs` (`FilePath?`, `MediaType?`), `IMediaStore.cs` |

### Block B — The data layer (write at git `d96de42b` or after Phase 4)

*The reader wants to know: where does data live and how does it get there?*

| # | Episode opening | Tracks | Status | Key files |
|---|-----------------|--------|--------|-----------|
| L005 | Why are there two databases? Couldn't one SQLite file hold everything? | arch | planned | `DECISIONS.md` D-001, D-015, D-027; `Data/Migrations/` |
| L006 | `SqliteJobQueue` implements `IJobQueue`. This is the "repository pattern" — what problem does it solve, and what goes wrong without it? | pattern, csharp | planned | `Core/Abstractions/IJobQueue.cs`, `Data/SqliteJobQueue.cs` |
| L007 | The `DequeueAsync` method does `BEGIN IMMEDIATE` inside an `await`. Raw SQL inside C# async code — why, and why not use an ORM? | arch, csharp | planned | `Data/SqliteJobQueue.cs` lines 46–102 |
| L008 | Schema migrations: why are there numbered `.sql` files and why are they append-only? What happens when you edit an existing migration? | arch | planned | `Data/Migrations/SQLite/`, `DatabaseMigrator.cs`, `DECISIONS.md` D-005, D-020 |

### Block C — The pipeline engine (write at git `d96de42b`)

*The reader traces a job from creation to completion and discovers the event system.*

| # | Episode opening | Tracks | Status | Key files |
|---|-----------------|--------|--------|-----------|
| L009 | `PipelineWorker` is a `BackgroundService`. It runs forever in the background, polling every 100ms. How does .NET support "run this forever"? | csharp, pattern | planned | `Core/Pipeline/PipelineWorker.cs` |
| L010 | `LocalSequentialRunner` implements `IJobRunner`. There's a `SemaphoreSlim` — what is it, and why is one slot for GPU but N slots for CPU? | csharp, pattern | planned | `Core/Pipeline/LocalSequentialRunner.cs` |
| L011 | After a job finishes, `JobCompletedEvent` fires. `DagEngine` receives it. They're not directly connected — who delivers the message? | pattern | planned | `Core/Events/`, `Core/Pipeline/DagEngine.cs`, `LocalSequentialRunner.cs` |
| L012 | `DagEngine` decides what to run next based on what's already completed. This is a DAG (directed acyclic graph) — what does that mean and why is it the right model here? | pattern, arch, repo | planned | `Core/Pipeline/DagEngine.cs`, `DECISIONS.md` D-023 |

### Block D — The plugin system (write after Phase 4 + Phase 6)

*The reader wants to understand how plugins connect to the core without the core knowing about them.*

| # | Episode opening | Tracks | Status | Key files |
|---|-----------------|--------|--------|-----------|
| L013 | `IInProcessPlugin` vs `IPlugin` vs `ExternalPluginInfo`. Three things that sound like "plugin" — what's the difference? | csharp, arch | planned | `Core/Abstractions/IPlugin.cs`, `IInProcessPlugin.cs`, `Core/Models/ExternalPluginInfo.cs` |
| L014 | Core never makes HTTP calls to plugins. Plugins call core. Isn't this backwards? (D-021 and why it was the right call.) | arch | planned | `DECISIONS.md` D-021, D-022; `docs/architecture.md §Plugin System` |
| L015 | `ExifPlugin` is a C# class. The CLIP embedder is Python. Both look the same to the pipeline. How? | arch, pattern, repo | planned | `Plugins.Exif/ExifPlugin.cs`, `DECISIONS.md` D-008, D-014 |

### Block E — End-to-end (write after Phase 5–6 are implemented)

*The reader wants to follow a real photo from disk to searchable result.*

| # | Episode opening | Tracks | Status | Write after |
|---|-----------------|--------|--------|-------------|
| L016 | Trace a single JPEG from `FileScanner.ScanAsync` to a completed thumbnail job — every step, every object, every database row | repo | planned | Phase 5+6 |
| L017 | Trace a text search query: "sunset beach" → CLIP embed → pgvector ANN → file IDs → thumbnails | repo, arch | planned | Phase 9 |
| L018 | The debug endpoint at `:7701` — walk through every route, what each one tells you, and how to use it to diagnose a stuck job | repo | planned | Phase 4 |

### Block F — Advanced C# (write when relevant pattern first appears)

*These don't have a fixed position — insert them when the reader would first need them.*

| # | Topic | Tracks | Status | Fits after |
|---|-------|--------|--------|------------|
| L019 | `IReadOnlyList<T>` — why not just `List<T>` on a public API? | csharp | planned | L001 or L006 |
| L020 | `sealed` — when marking a class final is a design statement, not an accident | csharp | planned | L010 or L013 |
| L021 | Polly retry pipelines — why retries without a library become spaghetti | csharp, pattern | planned | L010 |
| L022 | Dependency Injection: `Program.cs` wires everything together — how and why | csharp, pattern | planned | Phase 4 |

---

## Revision notes

*Record here when a planned lesson changes topic or order based on what actually got implemented.*

| Date | Change |
|------|--------|
| 2026-05-03 | Initial curriculum created. Revised from track-based to episode-based structure. |
