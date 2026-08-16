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
        return db.Folders.FromSqlInterpolated($"SELECT id, parent_id, name, path FROM folders WHERE id = {id}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken)
    {
        const string sql =
            """
               SELECT id AS "Id", name AS "Name", 'folder' AS "Type", 0 AS "SortOrder", lower(name) AS "SortName"
               FROM folders WHERE parent_id = {0}
               UNION ALL
               SELECT id AS "Id", name AS "Name", 'file' AS "Type", 1 AS "SortOrder", lower(name) AS "SortName"
               FROM files WHERE parent_id = {0}
               ORDER BY "SortOrder", "SortName", "Name"
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

    public Task AddFolderAsync(Folder folder, CancellationToken cancellationToken)
    {
        db.Folders.Add(folder);
        return Task.CompletedTask;
    }

    public Task AddFileAsync(File file, CancellationToken cancellationToken)
    {
        db.Files.Add(file);
        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(Folder folder, CancellationToken cancellationToken)
    {
        db.Folders.Remove(folder);
        return Task.CompletedTask;
    }

    public async Task DeleteFileAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM files WHERE id = {id}", cancellationToken);
        if (deleted == 0) throw new EntryNotFoundException("File not found.");
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string prefix, Guid? folderId, int limit,
        CancellationToken cancellationToken)
    {
        var escaped = prefix.ToLowerInvariant().Replace("\\", @"\\").Replace("%", "\\%").Replace("_", "\\_");
        var pattern = $"{escaped}%";

        const string allFilesSql =
            """
               SELECT file.id AS "Id", file.name AS "Name", dir.path || '/' || file.name AS "Path", file.parent_id AS "ParentId"
               FROM files file JOIN folders dir ON dir.id = file.parent_id
               WHERE LOWER(file.name) LIKE {0} ESCAPE '\'
               ORDER BY LOWER(file.name), file.name, file.id
               LIMIT {1}
            """;

        if (folderId is null)
            return await db.Database.SqlQueryRaw<SearchRow>(allFilesSql, pattern, limit)
                .Select(x => new SearchResultDto(x.Id, x.Name, x.Path, x.ParentId)).ToListAsync(cancellationToken);

        const string subtreeSql =
            """
                WITH RECURSIVE descendant_folder_ids AS (
                    SELECT id FROM folders WHERE id = {0}
                    UNION ALL
                    SELECT dir.id FROM folders dir JOIN descendant_folder_ids ancestor ON dir.parent_id = ancestor.id
                )
                SELECT file.id AS "Id", file.name AS "Name", dir.path || '/' || file.name AS "Path", file.parent_id AS "ParentId"
                FROM files file
                JOIN folders dir ON dir.id = file.parent_id
                WHERE file.parent_id IN (SELECT id FROM descendant_folder_ids)
                AND LOWER(file.name) LIKE {1} ESCAPE '\'
                ORDER BY LOWER(file.name), file.name, file.id
                LIMIT {2}
            """;

        return await db.Database.SqlQueryRaw<SearchRow>(subtreeSql, folderId.Value, pattern, limit)
            .Select(x => new SearchResultDto(x.Id, x.Name, x.Path, x.ParentId)).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
