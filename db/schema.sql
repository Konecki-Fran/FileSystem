CREATE TABLE folders
(
    id        UUID PRIMARY KEY,
    parent_id UUID NULL,
    name      VARCHAR(255) NOT NULL,
    path      VARCHAR(255) NOT NULL,
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

INSERT INTO folders (id, parent_id, name, path)
VALUES ('00000000-0000-0000-0000-000000000000', NULL, 'home', 'home');
