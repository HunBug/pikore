# PiKoRe Learning Materials

*A parallel track for turning this codebase into a teaching tool — without touching the codebase.*

---

## What this folder is

This folder contains numbered lessons tied to real code in this repository.
The lessons teach both **general software engineering skills** (C# language features, design patterns, architectural decisions) and **this specific codebase** — so reading through them also means you understand how and why PiKoRe is built the way it is.

This folder is completely separate from the rest of the project. Nothing here affects the build, the tests, or the runtime. It is documentation that grows alongside the code.

---

## Who it's for

The owner of this project (Boo Kumpli), who:
- Learns best by starting with a concrete problem, not abstract theory
- Wants to understand the code deeply, not just make it run
- Has intermediate coding experience but may not be steeped in C# idioms or enterprise patterns

---

## Teaching philosophy

**Problem first. Example second. Principle last.**

Every lesson opens with a real situation or failure mode you would encounter in *this repo*. It shows you the actual code. It explains what's going on. Only after that does it step back and state the general rule.

This is the opposite of how most textbooks work (define → example → exercise). It matches how you learn on a real project: you see something, you need to understand it, and then you go looking for the principle behind it.

Anti-patterns are as important as patterns. Lessons show what goes wrong when you ignore the advice, usually by pointing to a decision that was made (and documented in DECISIONS.md).

---

## How to use

**Reading sequentially** is best for building up foundational understanding. Early lessons assume nothing; later lessons build on earlier ones.

**Jumping to a topic** works too — each lesson is self-contained. Use the Tracks section of [CURRICULUM.md](CURRICULUM.md) to find lessons by topic area.

**Following the git history** is the deepest way to learn. Each lesson records the git commit hash at the time it was written. You can check out that exact state and read the code exactly as the lesson describes it:

```sh
git show d96de42b:src/PiKoRe.Core/Pipeline/DagEngine.cs
# or check out fully (careful — this detaches HEAD):
git checkout d96de42b
```

---

## Lesson index

| # | Title | Date | Tracks | Git ref |
|---|-------|------|--------|---------|
| L001 | Interfaces: why everything important has one | 2026-05-03 | csharp, arch | `a96b94c` |
| L002 | Records vs classes: data that travels through a pipeline | 2026-05-03 | csharp | `a96b94c` |
| L003 | CancellationToken: the polite way to stop | 2026-05-03 | csharp | `a96b94c` |
| L004 | Nullable types: making "no value" explicit | 2026-05-03 | csharp | `a96b94c` |

---

## Folder structure

```
learning/
  README.md              ← This file: overview + lesson index
  CURRICULUM.md          ← Planned lesson list, topics, status
  HOW_TO_ADD_LESSON.md   ← Instructions for new AI sessions creating lessons
  lessons/               ← Individual lesson files (created progressively)
    L001_YYYY-MM-DD_slug.md
    L002_YYYY-MM-DD_slug.md
    ...
```

---

## What's covered and what's planned

See [CURRICULUM.md](CURRICULUM.md) for the full plan with tracks, topic breakdown, and lesson status.

The rough arc:
1. **C# language** — records, interfaces, async/await, nullable types — using the repo's own code as examples
2. **Patterns** — repository, mediator, strategy, background service, DAG — each shown in context
3. **Architecture decisions** — why two databases, why no ORM, why the plugin system is built the way it is
4. **Repo tour** — end-to-end walkthroughs showing how data flows through the system

---

*Lessons are added during or after development sessions, tagged to the relevant commit.*
