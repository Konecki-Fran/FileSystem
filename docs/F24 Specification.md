# F24 Browser-Based File System - Project Specification

## 1. Purpose and Scope

This project implements a browser-based file system with a folder hierarchy and files represented by names only. The goal is a small, maintainable system that demonstrates sound frontend, backend, database, API, validation, error handling, concurrency, and testing practices without adding unnecessary production infrastructure.

The application supports folder and file creation, navigation, deletion, and prefix search. The search box is intended for search-as-you-type usage and returns at most 10 matching files.

Authentication and authorization are intentionally outside the project scope. Multiple users or browser sessions may access the same filesystem, so database constraints and transactions must still protect data consistency.

## 2. Functional Requirements

### Filesystem
- Display the contents of the current folder, including child folders and files.
- Navigate into child folders, back to the parent, and directly to the root/home folder.
- Create folders and files in the current folder.
- Delete files and folders.
- Deleting a non-empty folder recursively deletes its complete subtree after notifying the user.
- The root folder cannot be deleted.
- Moving entries and changing parent relationships are not supported.
### Search
- Search by filename prefix, using case-insensitive comparison.
- Search scopes are: all files in the filesystem, or the current folder and all of its descendants.
- Search results are limited to the top 10 files.
- Results are ordered lexicographically by filename to make the result set deterministic.
- An exact filename is naturally included when it is also a prefix match; no separate exact-search mode is exposed.
- Search executes while the user types.
### Naming
- Names must not be empty.
- Maximum name length is 255 characters.
- Path separator characters '/' and '\' are rejected.
- Leading and trailing whitespace is rejected or normalized consistently by validation.
- Names are displayed using their original case, but names are treated case-insensitively for uniqueness within the same parent.

## 3. Architecture and Technology Decisions

The solution follows a simple layered architecture: React frontend -> HTTP API -> ASP.NET + C# backend service/business layer -> PostgreSQL. The layers are separated so that business rules can be tested independently of HTTP and database plumbing.
### Frontend
- React-based browser application.
- Desktop/browser usage only; no mobile adaptation is required.
### Backend
- REST-style HTTP API with JSON request and response bodies.
- Business logic is kept outside route/controller code.
- Database access is isolated behind a repository/data-access layer.
### Database
- PostgreSQL is used because the filesystem is hierarchical and benefits from relational integrity, transactions, constraints, and indexed search.
### Key decisions
- UUID is the only identifier and is the primary key; a second numeric/internal identifier is not required.
- Files and folders remain separate tables because they have clear, different responsibilities while sharing the same parent/name model.
- The service checks case-insensitive sibling-name uniqueness across files and folders before creation. The current schema also has a case-insensitive uniqueness index for folders; the service is the source of truth for the shared file/folder namespace.
- Caching is intentionally omitted from the implementation to avoid unnecessary complexity for the assignment.

## 4. Frontend Specification

### Main view
- Show the current folder contents in a filesystem-style list.
- Show a read-only path from root to the current folder.
- The path is display-oriented; long paths are represented with an ellipsis and the final path segment portion so the UI remains bounded.
- Provide controls for parent navigation and returning to home/root.
### Create flow
- A create-entry dialog lets the user choose whether to create a folder or file and contains a validated name field.
- Submit with an explicit action or Enter; cancel by the cancel/close action or click-away.
- Validation covers empty names, maximum length, forbidden characters, whitespace rules, and duplicate names within the current parent.
- Pasted input is validated in the same way as typed input.
### Delete flow
- Delete is disabled when no entry is selected.
- Deleting a file requires confirmation.
- Deleting a folder clearly states that all descendants will also be deleted, then requires confirmation.
### Search flow
- Provide a search input and a scope selector with two options: all files, or current folder plus descendants.
- Search text is trimmed, must be 1–255 characters after trimming, and rejects path separator characters (`/` and `\\`) before a request is sent.
- Changing or extending the search text clears stale results while a new request is in progress.
- The client associates each response with the search prefix used to produce it and displays results only when that prefix still matches the current input.
- Search requests should be debounced or otherwise controlled to avoid unnecessary requests while typing.
- Search errors are surfaced to the user without breaking folder navigation.

## 5. Backend and Business Rules

### Folder operations
- Creating a folder sets its immutable parent relationship at creation time.
- A folder is addressed by UUID and has a name and nullable parent UUID.
- The single root folder has parent_id = NULL, is named 'home', and cannot be deleted.
- Opening '/' resolves the root folder and redirects the client to the root UUID route.
- Opening a folder returns enough data for content display and path navigation; parent relationships are used to walk the hierarchy.
### File operations
- A file contains only its UUID, parent UUID, and name; no file content is stored.
- Files cannot have children.
### Deletion
- File deletion removes the file in a transaction.
- Folder deletion removes the folder and every descendant file/folder in a transaction.
- Root deletion is rejected.
### Concurrency and consistency
- Create operations validate names and check existing sibling files and folders before insertion.
- The API rejects duplicate normalized sibling names with `409 NAME_ALREADY_EXISTS`.
- Parent relationships are immutable because moving entries is out of scope; cycle detection is therefore unnecessary.
### Error handling
- Invalid request: HTTP 400.
- Missing resource: HTTP 404.
- Attempt to delete the root or another business-rule conflict: HTTP 409.
- Database/internal failure: HTTP 500.
- Request cancellation: HTTP 499.
- Errors use a consistent JSON structure containing an error code and human-readable message.
### Transactions
- Create: validate -> insert -> save changes.
- Folder delete: delete the folder -> database foreign-key cascades delete its subtree -> save changes.
- File delete: issue a single delete statement and report `404` when no row was removed.

## 6. Data Model and Persistence

### Folder

| Property  | Type   | Constraints / meaning                                                                              |
| --------- | ------ | -------------------------------------------------------------------------------------------------- |
| id        | UUID   | Primary key                                                                                        |
| parent_id | UUID   | Nullable FK to folder.id; NULL only for root                                                       |
| name      | String | Required; max 255; path separators forbidden; service checks case-insensitive uniqueness across file and folder siblings |
| path      | String | Display-oriented path value, max 255 characters; constructed when the folder is created            |

### File

| Property | Type | Constraints / meaning |
| --- | --- | --- |
| id | UUID | Primary key |
| parent_id | UUID | Required FK to folder.id |
| name | String | Required; max 255; path separators forbidden; service checks case-insensitive uniqueness across file and folder siblings |

### Indexes and constraints
- The schema has a case-insensitive unique index on folder siblings (`parent_id`, `LOWER(name)`).
- The service also checks the `files` table so files and folders share one logical sibling namespace.
- Search uses parameterized SQL and a recursive CTE only when a folder scope is requested.
### Path handling
- The stored folder path is a UI-oriented convenience, not the authoritative hierarchy; the parent relationships remain authoritative.
- When a displayed path exceeds 255 characters, it is represented as an ellipsis followed by the final path portion.
- Search results include path context derived from the folder hierarchy so users can distinguish identically named files in different locations.

## 7. API Contract

| Method | Endpoint                     | Purpose                                                                      |
| ------ | ---------------------------- | ---------------------------------------------------------------------------- |
| GET    | /                            | Redirect to the root folder URL                                              |
| GET    | /folders/{id}                | Get folder metadata and immediate contents                                   |
| POST   | /folders/{id}                | Create a file or folder in the target folder; request contains name and type |
| DELETE | /folders/{id}                | Delete a folder recursively; root cannot be deleted                          |
| DELETE | /files/{id}                  | Delete a file                                                                |
| GET    | /search?prefix={prefix}&limit=10 | Prefix search across the entire filesystem                               |
| GET    | /search?prefix={prefix}&folder={id}&limit=10 | Prefix search in the specified folder subtree              |

The create request identifies the requested entry type explicitly (`Folder` or `File`) and supplies the entry name. Search requests supply the required search prefix and an optional folder scope. The server is authoritative for validation, limits, ordering, and data consistency.

Search prefixes follow the same trimming, length, and path-separator validation rules as entry names.

Search responses contain the matching file identity, filename, and path context required by the UI. The response is bounded by the requested maximum and the server applies the hard top-10 requirement.

## 8. Non-Functional Requirements and Constraints

### Required
- The project must build and run in debug mode.
- The solution must be delivered as a Git repository.
- A README must explain setup, configuration, local execution, database startup, and deployment/run instructions.
- Docker Compose supports two local execution modes: PostgreSQL-only for IDE development, and the complete PostgreSQL, API, and frontend stack. A test Compose overlay also loads manual-test seed data. Docker is optional under the assignment.
- Requests must be validated before business logic is executed.
- Database failures must be handled gracefully and must not produce misleading successful responses.
- Application logging records unhandled request errors without leaking sensitive data.
- The implementation should remain understandable and maintainable rather than optimizing for unnecessary infrastructure.
### Observability
- Basic logging is required.
- Production-level monitoring, dashboards, sophisticated metrics, and distributed tracing are intentionally out of scope.
### Security scope
- Authentication and authorization are not implemented, per the assignment.
- No user-specific filesystem ownership is modeled.
- Distributed rate limiting is omitted because there is no identity model and it is not required for the assignment.

## 9. Testing Strategy

### Backend unit tests
- Create file and folder.
- Duplicate sibling name.
- Invalid name validation.
- File deletion and recursive folder deletion.
- Search prefix, scope, limit, ordering, and case-insensitive behavior.
- Root behavior and root deletion rejection.
### Integration tests
- API -> service -> PostgreSQL workflows.
- Database uniqueness constraints and concurrent-create behavior.
- Transactions and rollback behavior.
- Recursive deletion.
- Search queries and returned data.
### Frontend tests
- Folder navigation and root navigation.
- Create/delete dialogs and validation behavior.
- Search input, request control/debounce, stale-result prevention, scope selection, and result rendering.
- User-visible error handling.

## 10. Risks and Edge Cases
- A non-empty folder deletion can remove large amounts of data; the UI must explicitly communicate recursive deletion before confirmation.
- Concurrent creates can race between client-side validation and insertion; the database constraint is the final authority.
- Search responses can arrive out of order while typing; the client must discard stale results.
- Very long folder paths can exceed display limits; path presentation must remain bounded without corrupting the underlying hierarchy.
- Repeated names in different folders are expected; search results must include sufficient path context to distinguish them.
- Invalid or missing UUIDs and missing resources must return consistent 400/404 responses rather than database errors.
- Database connection loss or transaction failure must not leave the API reporting a successful mutation.
- Search must be bounded to 10 results at the database/API layer rather than fetching all matches into the browser.
- The assignment is framed as large-scale, but no artificial latency target is imposed because end-to-end latency depends on deployment conditions. The implementation should instead use efficient indexed queries and avoid unnecessary round trips.

## 11. Deliberate Scope Exclusions and Future Improvements

### Excluded from the current implementation
- Authentication and authorization.
- Rename operations.
- Moving entries between folders.
- File content storage.
- Caching.
- Distributed rate limiting.
- Retry infrastructure and exponential backoff.
- Sophisticated monitoring and production dashboards.
### Potential future improvements
- Introduce authentication/authorization and user-specific roots or permissions.
- Add move/rename operations with stronger hierarchy-management rules.
- Evaluate a unified filesystem-entry table if the domain grows to require one namespace across files and folders.
- Add caching for repeated searches if real usage demonstrates a measurable benefit.
- Add richer metrics, tracing, and production monitoring.
- Add more advanced search features such as pagination, fuzzy matching, or richer query syntax if product requirements evolve.
The implementation intentionally prioritizes correctness, clear boundaries, relational integrity, predictable behavior, and maintainability over speculative production features.
