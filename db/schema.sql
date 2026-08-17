CREATE TABLE folders
(
    id        UUID PRIMARY KEY,
    parent_id UUID NULL,
    name      VARCHAR(255) NOT NULL,
    CONSTRAINT fk_folders_parent
        FOREIGN KEY (parent_id)
            REFERENCES folders (id)
            ON DELETE CASCADE
);

CREATE TABLE files
(
    id        UUID PRIMARY KEY,
    parent_id UUID         NOT NULL,
    name      VARCHAR(255) NOT NULL,
    CONSTRAINT fk_files_parent
        FOREIGN KEY (parent_id)
            REFERENCES folders (id)
            ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_folders_parentid_name
    ON folders(parent_id, lower (name));

CREATE UNIQUE INDEX IF NOT EXISTS ix_files_parentid_name
    ON files(parent_id, lower (name));

CREATE INDEX IF NOT EXISTS ix_files_name_prefix
    ON files ((lower(name) COLLATE "C") text_pattern_ops);

CREATE UNIQUE INDEX IF NOT EXISTS ix_folders_single_root
    ON folders ((parent_id IS NULL))
    WHERE parent_id IS NULL;

-- Files and folders share one logical sibling namespace. The advisory lock
-- serializes concurrent inserts into different tables; the indexes above
-- enforce same-table uniqueness.
CREATE OR REPLACE FUNCTION enforce_sibling_name_uniqueness()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_advisory_xact_lock(
        hashtextextended(NEW.parent_id::text || ':' || lower(NEW.name), 0));

    IF TG_TABLE_NAME = 'folders' AND EXISTS (
        SELECT 1 FROM files
        WHERE parent_id = NEW.parent_id AND lower(name) = lower(NEW.name)
    ) THEN
        RAISE EXCEPTION 'An entry with this name already exists in the folder.'
            USING ERRCODE = '23505', CONSTRAINT = 'uq_entries_parent_name';
    END IF;

    IF TG_TABLE_NAME = 'files' AND EXISTS (
        SELECT 1 FROM folders
        WHERE parent_id = NEW.parent_id AND lower(name) = lower(NEW.name)
    ) THEN
        RAISE EXCEPTION 'An entry with this name already exists in the folder.'
            USING ERRCODE = '23505', CONSTRAINT = 'uq_entries_parent_name';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER folders_sibling_name_uniqueness
    BEFORE INSERT OR UPDATE OF parent_id, name ON folders
    FOR EACH ROW EXECUTE FUNCTION enforce_sibling_name_uniqueness();

CREATE TRIGGER files_sibling_name_uniqueness
    BEFORE INSERT OR UPDATE OF parent_id, name ON files
    FOR EACH ROW EXECUTE FUNCTION enforce_sibling_name_uniqueness();

INSERT INTO folders (id, parent_id, name)
VALUES ('00000000-0000-0000-0000-000000000000', NULL, 'home');
