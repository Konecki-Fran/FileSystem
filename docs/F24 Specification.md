# F24 Browser-Based File System

## Scope

This project implements the interview assignment as a small React, ASP.NET Core, and PostgreSQL application. Files contain only a name. Authentication, authorization, file contents, rename, and move operations are outside scope.

## Supported behavior

- Browse from the root folder through child folders and return through parent/home navigation.
- Create files and folders in the current folder.
- Delete files and recursively delete folder subtrees after confirmation.
- Search for the top 10 files whose names start with a value across the filesystem.
- Search for an exact filename among the immediate files in the current folder.
- Match searches and sibling-name uniqueness case-insensitively while preserving original casing.
- Reject empty names, names longer than 255 characters, and `/` or `\`.

## Design

The application follows a deliberately small layered design:

`React UI -> HTTP controllers -> FileSystemService -> repository -> PostgreSQL`

Controllers handle transport concerns, the service owns business rules, and the repository contains database queries. PostgreSQL foreign keys recursively cascade folder deletion.

Folders and files are stored separately. Both tables have case-insensitive sibling indexes. Database triggers take a transaction-scoped advisory lock and check the other table, ensuring concurrent file/folder creates cannot violate their shared sibling namespace. Duplicate conflicts are returned as `409 NAME_ALREADY_EXISTS`.

Folder paths are not persisted. They are calculated from authoritative `parent_id` relationships using recursive queries. Display paths longer than 255 characters are abbreviated to `...` plus their final 252 characters.

## API

- `GET /` redirects to the root folder.
- `GET /folders/{id}` returns folder metadata, its derived display path, and immediate children.
- `POST /folders/{id}` creates a file or folder and returns `201` with the created entry.
- `DELETE /folders/{id}` recursively deletes a non-root folder.
- `DELETE /files/{id}` deletes a file.
- `GET /search?prefix=x&mode=PrefixAll&limit=10` performs global prefix search.
- `GET /search?prefix=x&mode=ExactCurrent&folder={id}&limit=10` performs exact search in the current folder.

Errors use `{ "error": { "code": "...", "message": "..." } }`. Invalid requests return 400, missing resources 404, business/duplicate conflicts 409, and unexpected database failures 500.

## Consistency and performance

- UUIDs are primary keys.
- Parent relationships are immutable, so cycles cannot be introduced through the API.
- Foreign keys protect hierarchy integrity and cascade deletion.
- Creation is protected against concurrent same-name requests at the database layer.
- Global prefix search uses a dedicated expression index, limits matches before deriving their paths, treats SQL wildcard characters literally, and orders deterministically.
- Stored paths and caches are intentionally avoided.

## Tests

Backend unit tests cover service rules with a fake repository. PostgreSQL integration tests cover:

- concurrent cross-type sibling creation;
- recursive deletion;
- prefix ordering, exact-current scope, and wildcard escaping;
- transaction rollback.

HTTP integration tests exercise the running ASP.NET Core pipeline against PostgreSQL and verify creation/deletion status codes, duplicate-conflict JSON, root and missing-resource errors, and search validation.

Frontend tests cover navigation, creation, deletion confirmation, validation, both search modes, and error presentation. Playwright provides a full-stack smoke test against PostgreSQL. GitHub Actions runs formatting, builds, unit/integration tests, frontend checks, and Playwright.

## Deliberate exclusions

Authentication, authorization, file content, rename/move operations, caching, distributed rate limiting, metrics infrastructure, and production deployment automation are not required by the assignment and are intentionally omitted.
