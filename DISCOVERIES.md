# PiKoRe — Discoveries

*Use this file to document non-obvious findings about the codebase, infrastructure, or domain. Primarily written by AI agent sessions for future sessions. Updated as the codebase grows.*

*Format: finding, which session/date discovered it, why it matters.*

---

## Architecture & structure

*(empty — implementation not started)*

---

## Database

*(empty)*

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

*(empty)*

---

*This file grows as development progresses. When a session discovers something non-obvious — a library quirk, a performance characteristic, an undocumented behavior — write it here so it is not rediscovered.*
