---
mode: agent
description: Create a new PiKoRe database migration
---

Create the next DbUp migration for PiKoRe.

1. Find the highest-numbered file in `src/PiKoRe.Data/Migrations/` to determine the next number.
2. Create `src/PiKoRe.Data/Migrations/{next_number}_{input:migrationName}.sql`

Migration to implement: ${input:description}

Rules:
- PostgreSQL syntax only (this is not SQLite)
- Include `-- Migration: {name}` as the first line comment
- Use `IF NOT EXISTS` / `IF EXISTS` guards where appropriate so the migration is safe to inspect manually
- For new tables: include `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- For vector columns: use pgvector type `vector(512)` — ensure the `vector` extension is enabled (it is, from migration 0001)
- No destructive operations without an explicit comment explaining why it is safe
- After creating the file, add a note in `CURRENT_STATE.md` about the new migration
