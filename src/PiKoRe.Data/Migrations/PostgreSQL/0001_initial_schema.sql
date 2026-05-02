CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS thumbnails (
    file_id    UUID NOT NULL,
    size_class TEXT NOT NULL,
    data_path  TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (file_id, size_class)
);

CREATE TABLE IF NOT EXISTS metadata (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_id       UUID NOT NULL,
    key           TEXT NOT NULL,
    value         TEXT NOT NULL,
    source_plugin TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (file_id, key, source_plugin)
);

CREATE TABLE IF NOT EXISTS tags (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_id       UUID NOT NULL,
    label         TEXT NOT NULL,
    confidence    REAL NOT NULL,
    source_plugin TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (file_id, label, source_plugin)
);

CREATE TABLE IF NOT EXISTS embeddings (
    file_id    UUID NOT NULL,
    model_id   TEXT NOT NULL,
    vector     vector(512),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (file_id, model_id)
);

CREATE TABLE IF NOT EXISTS faces (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_id    UUID NOT NULL,
    bbox_json  TEXT NOT NULL,
    embedding  vector(512),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS persons (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name          TEXT NOT NULL,
    cover_face_id UUID,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS face_person (
    face_id    UUID NOT NULL,
    person_id  UUID NOT NULL,
    confidence REAL NOT NULL DEFAULT 1.0,
    PRIMARY KEY (face_id, person_id)
);

CREATE TABLE IF NOT EXISTS scores (
    file_id       UUID NOT NULL,
    key           TEXT NOT NULL,
    value         REAL NOT NULL,
    source_plugin TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (file_id, key, source_plugin)
);

CREATE TABLE IF NOT EXISTS descriptions (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_id       UUID NOT NULL,
    text          TEXT NOT NULL,
    source_plugin TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_embeddings_vector ON embeddings USING hnsw (vector vector_cosine_ops);
CREATE INDEX IF NOT EXISTS idx_tags_file_id      ON tags (file_id);
CREATE INDEX IF NOT EXISTS idx_metadata_file_id  ON metadata (file_id);
CREATE INDEX IF NOT EXISTS idx_faces_file_id     ON faces (file_id);
