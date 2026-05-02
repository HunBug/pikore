# MCP Server Configuration

Two config files provide the same set of MCP servers to both agents:

| File | Used by |
|------|---------|
| `.vscode/mcp.json` | GitHub Copilot agent mode |
| `.mcp.json` | Claude Code (project root, **not** `.claude/mcp.json`) |

> **Important:** Claude Code reads `.mcp.json` from the **project root**. Placing it inside
> `.claude/mcp.json` is silently ignored — the servers will not load.

---

## Configured servers

### `memory` — `@modelcontextprotocol/server-memory`

In-memory key/value store that lives for the duration of a session. Use it to
stash intermediate findings, file lists, or notes when working across multiple
tool calls. Automatically discarded when the agent session ends — nothing
persists to disk.

### `github` — `@modelcontextprotocol/server-github`

Gives the agent read/write access to the `HunBug/pikore` GitHub repository:
issues, pull requests, commits, CI checks, labels. Required env var:

| Variable | Where to get it |
|----------|----------------|
| `GITHUB_PERSONAL_ACCESS_TOKEN` | GitHub → Settings → Developer settings → Personal access tokens → Fine-grained token. Scopes needed: `Contents` (read), `Issues` (read/write), `Pull requests` (read/write). |

The config files reference the token via `"${GITHUB_PERSONAL_ACCESS_TOKEN}"`. This placeholder
is **not** expanded from your shell profile (`.zprofile`, `.bashrc`) when Claude Code runs
inside the VSCode extension — the extension host has its own minimal environment.

**How to supply the token so Claude Code picks it up:**

Create `~/.claude/env` (plain text, no quotes, no `export`):
```
GITHUB_PERSONAL_ACCESS_TOKEN=your-token-here
```

Claude Code reads this file on startup and makes its contents available to all MCP servers.
This is the recommended approach — the token stays out of any committed file.

### `postgres` — `@modelcontextprotocol/server-postgres`

Read-only access to the local dev PostgreSQL instance (pgvector, analysis data).
Useful for inspecting table contents, checking migration state, or running ad-hoc
queries during development.

Connection string (matches `docker-compose.yml`):
```
postgresql://pikore:pikore_dev@localhost:5432/pikore
```

Requires `docker compose up -d` to be running before the agent starts.

### `sqlite` — `mcp-server-sqlite` (Python / uvx)

Access to the internal framework SQLite database (job queue, plugin registry,
pipeline config). The DB file lives inside the gitignored `workdir/` directory
so it never gets committed.

| Config file | Path used |
|-------------|-----------|
| `.vscode/mcp.json` | `${workspaceFolder}/workdir/pikore.db` |
| `.mcp.json` | `/home/akoss/Downloads/priv/code/pikore/workdir/pikore.db` (absolute) |

The file is created automatically by `DatabaseMigrator.MigrateSqlite()` the
first time the app runs. Until then, querying it will return an empty result or
a "file not found" error — that is expected.

If you move the repo, update the absolute path in `.mcp.json`.

Invoked via `uvx` (uv tool runner). The npm package `@modelcontextprotocol/server-sqlite`
no longer exists; `uvx` downloads and caches the Python package automatically.

### `fetch` — `mcp-server-fetch` (Python / uvx)

Lets the agent pull external URLs — useful for reading Avalonia docs, MediatR
API references, Ollama model pages, or pgvector documentation inline during a
session. No configuration required.

Invoked via `uvx`. The npm package `@modelcontextprotocol/server-fetch` no
longer exists; `uvx` downloads and caches the Python package automatically.

---

## The `workdir/` directory

`workdir/` is tracked in git (via `workdir/.gitkeep`) but its contents are
gitignored. Use it freely for:

- `pikore.db` — dev SQLite database
- scratch SQL scripts
- test media samples
- any other local-only files that shouldn't be committed

---

## Adding or removing a server

1. Add/remove the server block from **both** `.vscode/mcp.json` and
   `.mcp.json`.
2. Add a row to the table above and a section explaining what it does and any
   required env vars.
3. If the server needs a token, use a placeholder value (`"YOUR_X_TOKEN"`) in
   the committed file and document where to get it here.

---

## Installation note

No packages need to be installed globally.

- `memory`, `github`, `postgres` — invoked via `npx -y …`, downloaded on first
  use and cached in the npm cache. Requires **Node.js** in PATH (`node --version`).
  On Arch Linux: `sudo pacman -S nodejs npm`.
- `fetch`, `sqlite` — invoked via `uvx …`, downloaded on first use and cached
  by uv. Requires **uv** in PATH (`uvx --version`).
  On Arch Linux: `sudo pacman -S uv` or `curl -LsSf https://astral.sh/uv/install.sh | sh`.

> **Note:** `@modelcontextprotocol/server-github` and `@modelcontextprotocol/server-postgres`
> show a deprecation warning but still function. GitHub's official replacement
> (`github/github-mcp-server`) requires a separate binary download; the Postgres
> replacement (`uvx postgres-mcp-server`) is a drop-in if the npm package stops
> working.
