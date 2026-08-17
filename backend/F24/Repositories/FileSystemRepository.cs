using F24.Models.DTOs;
using F24.Models.Entities;
using F24.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using File = F24.Models.Entities.File;

namespace F24.Repositories;

public sealed class FileSystemRepository(AppDbContext db) : IFileSystemRepository
{
    public Task<Folder?> GetFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Folders.FromSqlInterpolated($"SELECT id, parent_id, name FROM folders WHERE id = {id}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<string> GetFolderPathAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql =
            """
               WITH RECURSIVE ancestors AS (
                   SELECT id, parent_id, name, 0 AS depth FROM folders WHERE id = {0}
                   UNION ALL
                   SELECT parent.id, parent.parent_id, parent.name, child.depth + 1
                   FROM folders parent JOIN ancestors child ON child.parent_id = parent.id
               )
               SELECT CASE WHEN length(string_agg(name, '/' ORDER BY depth DESC)) <= 255
                           THEN string_agg(name, '/' ORDER BY depth DESC)
                           ELSE '...' || right(string_agg(name, '/' ORDER BY depth DESC), 252)
                      END AS "Value"
               FROM ancestors
            """;

        return await db.Database.SqlQueryRaw<string>(sql, id).SingleAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken)
    {
        const string sql =
            """
               SELECT id AS "Id", name AS "Name", 'folder' AS "Type", 0 AS "SortOrder", lower(name) COLLATE "C" AS "SortName"
               FROM folders WHERE parent_id = {0}
               UNION ALL
               SELECT id AS "Id", name AS "Name", 'file' AS "Type", 1 AS "SortOrder", lower(name) COLLATE "C" AS "SortName"
               FROM files WHERE parent_id = {0}
               ORDER BY "SortOrder", "SortName", "Id"
            """;

        var rows = await db.Database.SqlQueryRaw<ChildRow>(sql, folderId).ToListAsync(cancellationToken);
        return rows.Select(row => new EntryDto(row.Id, row.Name, row.Type)).ToList();
    }

    public async Task<bool> EntryNameExistsAsync(Guid parentId, string name, CancellationToken cancellationToken)
    {
        const string sql =
            """
               SELECT EXISTS (
                   SELECT 1 FROM folders WHERE parent_id = {0} AND lower(name) = lower({1})
                   UNION ALL
                   SELECT 1 FROM files WHERE parent_id = {0} AND lower(name) = lower({1})
               ) AS "Value"
            """;
        return (await db.Database.SqlQueryRaw<ExistsRow>(sql, parentId, name).SingleAsync(cancellationToken)).Value;
    }

    public void AddFolder(Folder folder) => db.Folders.Add(folder);

    public void AddFile(File file) => db.Files.Add(file);

    public void DeleteFolder(Folder folder) => db.Folders.Remove(folder);

    public async Task DeleteFileAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM files WHERE id = {id}", cancellationToken);
        if (deleted == 0) throw new EntryNotFoundException("File not found.");
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, Guid? folderId, bool exact, int limit,
        CancellationToken cancellationToken)
    {
        var escaped = query.ToLowerInvariant().Replace("\\", @"\\").Replace("%", "\\%").Replace("_", "\\_");
        var pattern = $"{escaped}%";

        const string allFilesSql =
            """
               WITH RECURSIVE matches AS MATERIALIZED (
                   SELECT id, name, parent_id
                   FROM files
                   WHERE LOWER(name) COLLATE "C" LIKE {0} ESCAPE '\'
                   ORDER BY LOWER(name) COLLATE "C", name COLLATE "C", id
                   LIMIT {1}
               ),
               ancestors AS (
                   SELECT match.id AS file_id, folder.id, folder.parent_id, folder.name, 0 AS depth
                   FROM matches match JOIN folders folder ON folder.id = match.parent_id
                   UNION ALL
                   SELECT child.file_id, parent.id, parent.parent_id, parent.name, child.depth + 1
                   FROM ancestors child JOIN folders parent ON parent.id = child.parent_id
               ),
               result_paths AS (
                   SELECT file_id, string_agg(name, '/' ORDER BY depth DESC) AS path
                   FROM ancestors
                   GROUP BY file_id
               )
               SELECT match.id AS "Id", match.name AS "Name",
                      CASE WHEN length(path.path || '/' || match.name) <= 255
                           THEN path.path || '/' || match.name
                           ELSE '...' || right(path.path || '/' || match.name, 252) END AS "Path",
                      match.parent_id AS "ParentId"
               FROM matches match JOIN result_paths path ON path.file_id = match.id
               ORDER BY LOWER(match.name) COLLATE "C", match.name COLLATE "C", match.id
            """;

        if (!exact && folderId is null)
            return await db.Database.SqlQueryRaw<SearchRow>(allFilesSql, pattern, limit)
                .Select(x => new SearchResultDto(x.Id, x.Name, x.Path, x.ParentId)).ToListAsync(cancellationToken);

        if (!exact || folderId is null)
            throw new DomainException("INVALID_SEARCH_MODE", "Exact search requires a current folder.");

        const string exactCurrentSql =
            """
                WITH RECURSIVE matches AS MATERIALIZED (
                    SELECT id, name, parent_id
                    FROM files
                    WHERE parent_id = {0} AND LOWER(name) = LOWER({1})
                    ORDER BY LOWER(name) COLLATE "C", name COLLATE "C", id
                    LIMIT {2}
                ),
                ancestors AS (
                    SELECT match.id AS file_id, folder.id, folder.parent_id, folder.name, 0 AS depth
                    FROM matches match JOIN folders folder ON folder.id = match.parent_id
                    UNION ALL
                    SELECT child.file_id, parent.id, parent.parent_id, parent.name, child.depth + 1
                    FROM ancestors child JOIN folders parent ON parent.id = child.parent_id
                ),
                result_paths AS (
                    SELECT file_id, string_agg(name, '/' ORDER BY depth DESC) AS path
                    FROM ancestors
                    GROUP BY file_id
                )
                SELECT match.id AS "Id", match.name AS "Name",
                       CASE WHEN length(path.path || '/' || match.name) <= 255
                            THEN path.path || '/' || match.name
                            ELSE '...' || right(path.path || '/' || match.name, 252) END AS "Path",
                       match.parent_id AS "ParentId"
                FROM matches match JOIN result_paths path ON path.file_id = match.id
                ORDER BY LOWER(match.name) COLLATE "C", match.name COLLATE "C", match.id
            """;

        return await db.Database.SqlQueryRaw<SearchRow>(exactCurrentSql, folderId.Value, query, limit)
            .Select(x => new SearchResultDto(x.Id, x.Name, x.Path, x.ParentId)).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
