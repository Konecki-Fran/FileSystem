using F24.Models.DTOs;
using F24.Models.Entities;
using File = F24.Models.Entities.File;

namespace F24.Repositories;

public interface IFileSystemRepository
{
    Task<Folder?> GetFolderAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken);
    Task<bool> EntryNameExistsAsync(Guid parentId, string name, CancellationToken cancellationToken);
    Task AddFolderAsync(Folder folder, CancellationToken cancellationToken);
    Task AddFileAsync(File file, CancellationToken cancellationToken);
    Task DeleteFolderAsync(Folder folder, CancellationToken cancellationToken);
    Task DeleteFileAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string prefix, Guid? folderId, int limit,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}