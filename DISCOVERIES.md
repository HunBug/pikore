# PiKoRe — Discoveries

*Use this file to document non-obvious findings about the codebase, infrastructure, or domain. Primarily written by AI agent sessions for future sessions. Updated as the codebase grows.*

*Format: finding, which session/date discovered it, why it matters.*

---

## Architecture & structure

- **`AnalysisResult` has a companion `FaceResult` record**: `AnalysisResult.Faces` is `IReadOnlyList<FaceResult>?`, not a raw list of strings. `FaceResult` holds `BboxJson` and `Embedding`. Both are defined in `src/PiKoRe.Core/Models/AnalysisResult.cs`. The original plan listed `Faces` as a collection but didn't specify the element type — `FaceResult` was the natural choice to carry both bounding box and embedding per face. *Discovered: 2026-05 (Phase 2).*

- **`IUpgradeLog` adapter lives inside `DatabaseMigrator` as a private nested class**: `DatabaseMigrator.MicrosoftLogAdapter` bridges `Microsoft.Extensions.Logging.ILogger` to DbUp's `IUpgradeLog`. Both `MigrateSqlite` and `MigratePostgres` accept an `ILogger` parameter. When calling from tests, use `NullLogger.Instance`. *Discovered: 2026-05 (Phase 2).*

- **DbUp script filtering uses embedded resource names**: Scripts are selected by `name.Contains(".SQLite.")` and `name.Contains(".PostgreSQL.")`. The embedded resource names are generated from the folder path (`PiKoRe.Data.Migrations.SQLite.0001_initial_schema.sql`). New migration files must go in the same folder structure to be picked up automatically. *Discovered: 2026-05 (Phase 2).*

---

## Database

- **SQLite schema uses TEXT for all IDs and timestamps**: All GUIDs are stored as `TEXT` (not BLOB) and all timestamps as ISO-8601 `TEXT`. This matches `DateTimeOffset.ToString("O")` round-trip and is the simplest portable choice for SQLite. When mapping back to C# in `SqliteJobQueue`, parse with `Guid.Parse` and `DateTimeOffset.Parse`. *Discovered: 2026-05 (Phase 2).*

- **PostgreSQL schema: `embeddings.vector` is `vector(512)` fixed dimension**: The HNSW index `idx_embeddings_vector` uses `vector_cosine_ops`. ANN queries must use the `<=>` cosine distance operator. If a future plugin produces embeddings of a different dimension (e.g. 768 from a CLIP-L variant), the `model_id` column disambiguates rows but the `vector(512)` column type will reject them — a new migration adding a second embeddings table or changing the column type would be needed. *Discovered: 2026-05 (Phase 2).*

- **`MigratePostgres` calls `EnsureDatabase.For.PostgresqlDatabase` before running scripts**: This creates the database if it does not exist. `MigrateSqlite` does not call the equivalent — SQLite creates the file automatically on first connection. *Discovered: 2026-05 (Phase 2).*

---

## Plugin protocol

*(empty)*

---

## External dependencies / quirks

- **Avalonia 11.1.0 → Tmds.DBus.Protocol 0.16.0 (NU1903 high-severity vuln)**: `Tmds.DBus.Protocol` 0.16.0 is a transitive dependency pulled in by Avalonia 11.1.0. It carries a known high-severity vulnerability ([GHSA-xrw6-gwf8-vvr9](https://github.com/advisories/GHSA-xrw6-gwf8-vvr9)). This cannot be resolved by us directly — it requires Avalonia to update its own dependency. The NU1903 warning appears on every build. Monitor Avalonia releases for a fix. *Discovered: 2026-05 (Phase 1).*

- **`dotnet new sln` in .NET 10 SDK creates `.slnx`**: The new XML-based solution format (`.slnx`) is the default when creating solutions with the .NET 10 SDK. This is a breaking change from the classic `.sln` format but is transparent to the `dotnet` CLI. Older versions of some IDEs may not support it — check IDE compatibility before onboarding team members. *Discovered: 2026-05 (Phase 1).*

- **`avalonia.app` template defaults to `net8.0`**: Despite using the .NET 10 SDK, the `Avalonia.Templates` 11.1.0 package defaults the new project to `net8.0`. TFM must be manually corrected in `PiKoRe.UI/PiKoRe.UI.csproj` after creation. *Discovered: 2026-05 (Phase 1).*

- **DbUp-SQLite pulls in `Microsoft.Data.Sqlite.Core 9.0.x` as a transitive dep**: DbUp-SQLite 6.0.4 pins `Microsoft.Data.Sqlite.Core` to 9.0.10 while we also have `Microsoft.Data.Sqlite` 10.0.7 directly referenced. MSBuild resolves this without error (picks the highest compatible version), but it is worth watching for runtime conflicts. *Discovered: 2026-05 (Phase 1).*

---

## Gotchas and surprises

- **DbUp 6.x changed `IUpgradeLog`**: The old interface had `WriteError/WriteInformation/WriteWarning`. DbUp 6.x replaced these with `LogTrace/LogDebug/LogInformation/LogWarning/LogError(string)/LogError(Exception, string)`. Any `IUpgradeLog` adapter written for DbUp 5.x will not compile. *Discovered: 2026-05 (Phase 2).*

- **DbUp-SQLite extension method is `SqliteDatabase` (lowercase 'q')**: The method on `SupportedDatabases` is `SqliteDatabase`, not `SQLiteDatabase`. *Discovered: 2026-05 (Phase 2).*

- **Testcontainers 4.x `PostgreSqlBuilder` parameterless constructor is obsolete**: Pass the image string directly to the constructor (`new PostgreSqlBuilder("pgvector/pgvector:pg16")`) instead of chaining `.WithImage(...)`. The old form still compiles but emits a CS0618 warning. *Discovered: 2026-05 (Phase 2).*

- **Testcontainers PostgreSQL integration test requires `pgvector/pgvector:pg16` image**: The standard `postgres:latest` image does not ship the `vector` extension. Any test that runs the PostgreSQL migration (which calls `CREATE EXTENSION IF NOT EXISTS vector`) must use the pgvector image. *Discovered: 2026-05 (Phase 2).*

---

*This file grows as development progresses. When a session discovers something non-obvious — a library quirk, a performance characteristic, an undocumented behavior — write it here so it is not rediscovered.*
