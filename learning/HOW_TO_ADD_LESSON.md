# How to Add a New Lesson

*Instructions for AI sessions creating learning content.*
*Read this file fully before writing any lesson.*

---

## Purpose

This guide tells you exactly how to write a lesson for the `learning/` folder. Follow it carefully — the whole point of this system is that lessons are consistent, tied to the actual codebase, and follow a specific teaching style.

---

## When to add a lesson

A lesson is appropriate when:
- A new non-trivial language feature, pattern, or design concept has appeared in the codebase and has a good real-world example in this repo
- A significant architectural decision was made (or reversed) and understanding *why* requires more than reading DECISIONS.md
- You are starting a new development phase and there is a natural "what is happening here" story to tell

Do NOT add a lesson just to fill a slot in the curriculum. Quality over quantity.

---

## Pre-flight checklist

Before writing a lesson:

1. **Read [CURRICULUM.md](CURRICULUM.md)** — find the next lesson number, understand what's already been covered, and choose the best next topic given the current state of the code.

2. **Read all existing lessons** in `learning/lessons/` — so you don't repeat concepts already explained. New lessons may reference older ones.

3. **Get the current git hash:**
   ```sh
   git rev-parse HEAD
   ```
   This is the `{GIT_HASH}` you will embed in the lesson metadata.

4. **Read the relevant source files** — open the actual code the lesson will teach. The lesson should show real snippets from the repo, not invented examples.

5. **Check DECISIONS.md** — if the lesson covers an architectural choice, cite the relevant D-NNN entry.

---

## Naming convention

```
learning/lessons/L{NNN}_YYYY-MM-DD_{slug}.md
```

- `{NNN}` — zero-padded three-digit lesson number (L001, L002, ...)
- `YYYY-MM-DD` — the date the lesson was written (today's date, from context)
- `{slug}` — 2-4 lowercase words joined with underscores describing the topic

Examples:
```
L001_2026-05-03_interfaces.md
L002_2026-05-10_records_vs_classes.md
L003_2026-05-17_async_cancellation.md
```

---

## Lesson template

Copy this template exactly. Fill in every section. Do not skip any section.

The arc of every lesson: **problem encountered → naive attempt → real solution → general principle → back to practice**. The reader should feel like they're discovering the answer, not being lectured at.

```markdown
# L{NNN} — {Title}

> Lesson {NNN} | {YYYY-MM-DD} | Git: `{GIT_HASH}`
> Tracks: {comma-separated: csharp, pattern, arch, repo}
> Requires: {comma-separated lesson numbers, or "none"}

---

## You open the file and see...

[The concrete encounter. Show a short snippet — 5 to 15 lines — from the real repo.
Cite the file path and git hash immediately below it.
No explanation yet. Let the reader look at it and form a first guess.
Then ask the natural question they'd have: "What is this? Why is it written this way?"]

---

## First instinct: the naive approach

[Show what someone might write instead, without this knowledge.
Mark it clearly — use a comment `// naive — problems below` or a "Before:" sub-heading.
This should be concrete code or a concrete description of the wrong approach.
Don't be condescending — this is what a reasonable person would try first.]

---

## What breaks

[Explain what goes wrong with the naive approach. Be specific — not "it's bad" but
"this crashes when two workers dequeue simultaneously" or "this makes testing impossible
because you can't swap out the database in tests."
One or two concrete failure modes are more memorable than a list of abstract concerns.]

---

## What the repo actually does (and why)

[Now walk through the real code in detail. This is the longest section.
Show more of the real file if needed — still cite path and hash.
Explain line by line when necessary. Point to DECISIONS.md entries where relevant (cite D-NNN).
Use diagrams here if the structure benefits from it — see diagram guidelines.]

---

## The general idea

[Now and only now, state the abstract principle.
- What is this pattern or feature called?
- One-paragraph summary of when you use it and why.
- Keep this SHORT — the example above is the real teaching. This is the label for it.]

---

## Another place you'll see this

[Point to one or two other locations in this codebase where the same idea appears.
This reinforces that the lesson isn't a one-off curiosity — it's a recurring tool.
"You'll see the same pattern in X when we get to Phase N."]

---

## Try it

[2–3 exploration tasks. These are not coding exercises — they're reading/investigation tasks.
Examples:
- "Find every place in Core/Abstractions/ where this pattern appears."
- "Read D-NNN in DECISIONS.md. Which part of this lesson does that decision explain?"
- "Checkout the git hash for this lesson and run `dotnet test` — which test covers this code?"]
```

---

## Teaching style rules

**Show the problem before the solution.** The lesson opens with real code that looks confusing or incomplete. The naive approach section shows what someone would write without knowledge of the pattern. The failure section shows what breaks. Only then comes the actual solution.

**The naive approach is not wrong — it's reasonable.** Don't mock it. It's what a thoughtful person with incomplete information would write. The lesson explains why it falls short, not why only an idiot would write it.

**Concrete before abstract.** The code example comes before the explanation. The reader should look at the code first and try to form their own initial hypothesis, then the explanation confirms or corrects it.

**Show the real code, not a simplified toy.** Use actual file paths and actual snippets from the repo. If a snippet is too long, trim it — but mark the trim clearly (`// ... rest of method`).

**Don't explain what the code does in the abstract.** Show what it *is*, in context. The reader can see what the method does from the code; what they need is the *why*.

**"The general idea" section must be short.** The concrete sections (naive approach + real solution) are the teaching. The general idea is just a name and a summary — two to four sentences maximum.

**Write for someone who last coded six months ago.** Not for an expert. Don't assume knowledge of patterns, libraries, or C# idioms that haven't been covered in earlier lessons.

---

## Code snippet guidelines

Always cite file path and git hash above each snippet:

```
`src/PiKoRe.Core/Pipeline/DagEngine.cs` @ `d96de42b`
```

For multi-file examples, use a sub-heading per file.

For long files, only include the relevant section. Use `// ...` to indicate omitted lines:

```csharp
public sealed class LocalSequentialRunner : IJobRunner
{
    // ...
    public async Task<JobResult> RunAsync(Job job, CancellationToken ct)
    {
        // ... (constructor code omitted)
    }
}
```

---

## After writing the lesson

1. **Update the lesson index table in [README.md](README.md):**
   Add a new row: `| {NNN} | {Title} | {Date} | {Tracks} | {short git hash (7 chars)} |`

2. **Update the status in [CURRICULUM.md](CURRICULUM.md):**
   Find the row for this lesson topic and change the status from `planned` to `done`.

3. **Do not modify any other files in the repo.** The `learning/` folder is self-contained.

---

## Quality checklist before finishing

- [ ] Lesson starts with a concrete problem/situation, not a definition
- [ ] Code examples come from the actual repo at the cited git hash
- [ ] Every code snippet has a file path citation
- [ ] The anti-pattern section is filled out
- [ ] README.md lesson index is updated
- [ ] CURRICULUM.md status is updated
- [ ] No lessons are repeated — topics in earlier lessons are referenced, not re-explained
