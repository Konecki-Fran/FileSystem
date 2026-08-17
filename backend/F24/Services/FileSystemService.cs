using F24.Models.DTOs;
using F24.Models.Entities;
using F24.Models.Enums;
using F24.Models.Requests;
using F24.Repositories;
using F24.Services.Exceptions;
using File = F24.Models.Entities.File;

namespace F24.Services;

public sealed class FileSystemService(IFileSystemRepository repository)
{
    public async Task<FolderContentsDto> GetFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        var folder = await repository.GetFolderAsync(id, cancellationToken);
        if (folder is null) throw new EntryNotFoundException("Folder not found.");

        var path = await repository.GetFolderPathAsync(id, cancellationToken);
        var children = await repository.GetChildrenAsync(id, cancellationToken);
        return new FolderContentsDto(folder.Id, folder.Name, folder.ParentId, path, children);
    }

    public async Task<EntryDto> CreateAsync(Guid parentId, CreateEntryRequest request,
        CancellationToken cancellationToken)
    {
        var name = NameValidator.Normalize(request.Name);
        var type = request.Type;

        _ = await repository.GetFolderAsync(parentId, cancellationToken)
            ?? throw new EntryNotFoundException("Parent folder not found.");

        if (await repository.EntryNameExistsAsync(parentId, name, cancellationToken))
            throw new DuplicateNameException("An entry with this name already exists in the folder.");

        if (type == EntryType.Folder)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                ParentId = parentId,
                Name = name
            };
            repository.AddFolder(folder);
            await repository.SaveChangesAsync(cancellationToken);
            return new EntryDto(folder.Id, folder.Name, "folder");
        }

        var file = new File { Id = Guid.NewGuid(), ParentId = parentId, Name = name };
        repository.AddFile(file);
        await repository.SaveChangesAsync(cancellationToken);
        return new EntryDto(file.Id, file.Name, "file");
    }

    public async Task DeleteFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        var folder = await repository.GetFolderAsync(id, cancellationToken)
                     ?? throw new EntryNotFoundException("Folder not found.");
        if (folder.ParentId is null) throw new CannotDeleteRootException("The root folder cannot be deleted.");

        repository.DeleteFolder(folder);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(Guid id, CancellationToken cancellationToken)
    {
        await repository.DeleteFileAsync(id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, SearchMode mode, Guid? folderId, int limit,
        CancellationToken cancellationToken)
    {
        var normalized = NameValidator.Normalize(query);
        if (limit is < 1 or > 10)
            throw new DomainException("INVALID_LIMIT", "Limit must be between 1 and 10.");
        if (mode == SearchMode.ExactCurrent && folderId is null)
            throw new DomainException("INVALID_SEARCH_MODE", "Exact search requires a current folder.");
        if (!Enum.IsDefined(mode))
            throw new DomainException("INVALID_SEARCH_MODE", "Search mode is invalid.");
        if (mode == SearchMode.ExactCurrent &&
            await repository.GetFolderAsync(folderId!.Value, cancellationToken) is null)
            throw new EntryNotFoundException("Folder not found.");

        return await repository.SearchAsync(normalized, folderId, mode == SearchMode.ExactCurrent, limit,
            cancellationToken);
    }
}
