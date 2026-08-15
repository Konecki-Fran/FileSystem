using F24.Models.DTOs;
using F24.Repositories;

namespace F24.Services;

public sealed class FileSystemService(IFileSystemRepository repository)
{
    public async Task<FolderContentsDto?> GetFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        var folder = await repository.GetFolderAsync(id, cancellationToken);
        if (folder is null) return null;

        var children = await repository.GetChildrenAsync(id, cancellationToken);
        return new FolderContentsDto(folder.Id, folder.Name, folder.ParentId, folder.Path, children);
    }
}
