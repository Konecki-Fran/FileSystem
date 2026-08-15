using F24.Models.DTOs;
using F24.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace F24.Repositories;

public sealed class FileSystemRepository(AppDbContext db) : IFileSystemRepository
{
    public Task<Folder?> GetFolderAsync(Guid id, CancellationToken cancellationToken) =>
        db.Folders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken)
    {
        var folders = await db.Folders.AsNoTracking().Where(x => x.ParentId == folderId)
            .OrderBy(x => x.Name).Select(x => new EntryDto(x.Id, x.Name, "folder")).ToListAsync(cancellationToken);
        var files = await db.Files.AsNoTracking().Where(x => x.ParentId == folderId)
            .OrderBy(x => x.Name).Select(x => new EntryDto(x.Id, x.Name, "file")).ToListAsync(cancellationToken);
        return folders.Concat(files).ToList();
    }
}
