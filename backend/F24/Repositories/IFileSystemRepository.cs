using F24.Models.DTOs;
using F24.Models.Entities;

namespace F24.Repositories;

public interface IFileSystemRepository
{
    Task<Folder?> GetFolderAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken);
}
