# L002 — Records vs classes: data that travels through a pipeline

> Lesson 002 | 2026-05-03 | Git: `a96b94c`
> Tracks: csharp
> Requires: L001

---

## You open the file and see...

`src/PiKoRe.Core/Models/Job.cs` @ `a96b94c`

```csharp
public sealed record Job(
    Guid Id,
    Guid FileId,
    string Capability,
    JobStatus Status,
    Guid? PluginId,
    int Priority,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    string? Error,
    string? FilePath,
    string? MediaType
);
```

The word is `record`, not `class`. Same for `JobResult` and `IndexedFile`.

You could probably guess that `Job` describes a piece of work in the queue. But why is it a `record`? What's wrong with a `class`?

---

## First instinct: the naive approach

A reasonable first attempt at `Job` as a class:

```csharp
// naive — problems below
public class Job
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public string Capability { get; set; }
    public JobStatus Status { get; set; }
    // ... other properties
}
```

This is valid C#. Most tutorials write `class` by default, so it seems fine. You create one: `new Job { Id = Guid.NewGuid(), Capability = "thumbnail", ... }`. It compiles. It works.

So what breaks?

---

## What breaks

**Problem 1: mutation mid-flight.**

A `Job` object travels from `SqliteJobQueue.DequeueAsync` → `PipelineWorker` → `LocalSequentialRunner` → `DagEngine`. That's four different code locations reading the same object.

With a mutable class, any of them could accidentally write `job.Status = JobStatus.Completed`. Now the other readers see a modified object — and there was no database update, no log entry, and no explicit intent. The bug is silent.

With a record, all properties are `init`-only by default. There is no setter. The compiler refuses to let you mutate a `Job` after it's been created.

**Problem 2: equality by reference, not by value.**

With a class:

```csharp
var job1 = new Job { Id = someGuid, Capability = "thumbnail" };
var job2 = new Job { Id = someGuid, Capability = "thumbnail" };

Console.WriteLine(job1 == job2); // false — different objects in memory
```

Two jobs with identical data are not equal. This matters if you're deduplicating jobs or checking whether a result belongs to a specific job.

With a record, `==` compares all properties. Two `Job` records with the same data are equal.

---

## What the repo actually does (and why)

All three pipeline data models are records:

`src/PiKoRe.Core/Models/IndexedFile.cs` @ `a96b94c`

```csharp
public sealed record IndexedFile(
    Guid Id,
    string Path,
    long SizeBytes,
    DateTimeOffset MTime,
    string Hash,
    DateTimeOffset IngestedAt,
    string MediaType
);
```

`src/PiKoRe.Core/Models/JobResult.cs` @ `a96b94c`

```csharp
public sealed record JobResult(
    Guid JobId,
    Guid FileId,
    string Capability,
    string? MediaType,
    bool Success,
    string? Error,
    AnalysisResult? Result
);
```

When `SqliteJobQueue.DequeueAsync` reads a row from the database and needs to hand back a `Job` with `Status = Running`, it doesn't mutate the object — it constructs a fresh one:

```csharp
return new Job(
    Guid.Parse(row.Id),
    Guid.Parse(row.FileId),
    row.Capability,
    JobStatus.Running,         // ← new status
    row.PluginId is null ? null : Guid.Parse(row.PluginId),
    row.Priority,
    DateTimeOffset.Parse(row.CreatedAt),
    DateTimeOffset.Parse(now),
    row.Error,
    row.FilePath,
    row.MediaType);
```

The old state is not modified. The new state is a new object. This is the whole idea.

The C# `with` expression gives you a shortcut for this if you already have a record and want to change one field:

```csharp
var updated = existingJob with { Status = JobStatus.Running };
```

This creates a new `Job` with all fields copied from `existingJob`, except `Status`. The original is unchanged.

Here's a summary of what you get from `record` versus `class`:

| | `class` | `record` |
|---|---|---|
| Mutability | mutable by default | init-only by default |
| Equality | reference (same object?) | value (same data?) |
| Copy-with-change | manual copy constructor | `with` expression |
| Intended for | entities with identity and lifecycle | data that travels and is compared |

---

## The general idea

A **record** is a class optimized for representing immutable data. It compiles down to a regular class under the hood, but the C# compiler generates value-based equality, a pretty-printed `ToString`, and `with`-expression support automatically.

Use `record` for data that moves through your system without being modified: messages, results, requests, snapshots. Use `class` for things that have lifecycle, mutable state, or behaviour: services, workers, database connections.

In a pipeline, `Job` and `JobResult` are messages. They should be records.

---

## Another place you'll see this

`FileAnalysisDetails` in `src/PiKoRe.Core/Models/FileAnalysisDetails.cs` is also a record — it's the aggregated analysis snapshot returned from PostgreSQL. `AnalysisResult` and `AnalysisRequest` follow the same pattern. Any object that flows *through* the pipeline rather than *running* the pipeline will be a record.

---

## Try it

1. Open `src/PiKoRe.Core/Models/`. Which files use `record` and which use `class` (or `enum`)? Can you spot the pattern in what gets a record vs. what doesn't?

2. In `src/PiKoRe.Data/SqliteJobQueue.cs`, there's a `JobRow` record at the bottom of the file. This one is `private sealed record`. Why define a separate internal record just for reading from the database, instead of reusing `Job` directly?

3. Try this thought experiment: if `Job` were a mutable class, what would happen if `LocalSequentialRunner` set `job.Status = JobStatus.Completed` before calling `_jobQueue.MarkCompletedAsync`? Would the database be updated? Would the caller see the change?
