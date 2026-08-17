using F24.Models.DTOs;
using F24.Models.Entities;
using File = F24.Models.Entities.File;

namespace F24.Repositories;

public interface IFileSystemRepository
{
    Task<Folder?> GetFolderAsync(Guid id, CancellationToken cancellationToken);
    Task<string> GetFolderPathAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken);
    Task<bool> EntryNameExistsAsync(Guid parentId, string name, CancellationToken cancellationToken);
    void AddFolder(Folder folder);
    void AddFile(File file);
    void DeleteFolder(Folder folder);
    Task DeleteFileAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, Guid? folderId, bool exact, int limit,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
