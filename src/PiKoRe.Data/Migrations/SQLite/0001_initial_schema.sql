CREATE TABLE IF NOT EXISTS plugin_registry (
    id                    TEXT PRIMARY KEY,
    name                  TEXT NOT NULL UNIQUE,
    version               TEXT NOT NULL,
    endpoint              TEXT,
    capabilities_produced TEXT NOT NULL,  -- JSON array
    required_capabilities TEXT NOT NULL,  -- JSON array
    gpu_memory_mb         INTEGER NOT NULL DEFAULT 0,
    status                TEXT NOT NULL DEFAULT 'active',
    config_json           TEXT,
    registered_at         TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS file_index (
    id          TEXT PRIMARY KEY,
    path        TEXT NOT NULL UNIQUE,
    size_bytes  INTEGER NOT NULL,
    mtime       TEXT NOT NULL,
    hash        TEXT NOT NULL,
    ingested_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS job_queue (
    id         TEXT PRIMARY KEY,
    file_id    TEXT NOT NULL,
    capability TEXT NOT NULL,
    status     TEXT NOT NULL DEFAULT 'queued',
    plugin_id  TEXT,
    priority   INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    error      TEXT,
    FOREIGN KEY (file_id) REFERENCES file_index(id)
);

CREATE TABLE IF NOT EXISTS pipeline_config (
    id         TEXT PRIMARY KEY,
    dag_json   TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS system_config (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

PRAGMA journal_mode=WAL;
