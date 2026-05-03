-- Add media_type to file_index.
-- 'unknown' default covers any rows that predate this migration; FileScanner
-- will populate the correct value for all newly indexed files.
ALTER TABLE file_index ADD COLUMN media_type TEXT NOT NULL DEFAULT 'unknown';

-- Add supported_media_types to plugin_registry.
-- '["*"]' default means "accepts all types", which is safe for any legacy rows.
ALTER TABLE plugin_registry ADD COLUMN supported_media_types TEXT NOT NULL DEFAULT '["*"]';
