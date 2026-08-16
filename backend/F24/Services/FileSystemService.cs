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

        var children = await repository.GetChildrenAsync(id, cancellationToken);
        return new FolderContentsDto(folder.Id, folder.Name, folder.ParentId, folder.Path, children);
    }

    public async Task<EntryDto> CreateAsync(Guid parentId, CreateEntryRequest request,
        CancellationToken cancellationToken)
    {
        var name = NameValidator.Normalize(request.Name);
        var type = request.Type;

        var parent = await repository.GetFolderAsync(parentId, cancellationToken)
                     ?? throw new EntryNotFoundException("Parent folder not found.");

        if (await repository.EntryNameExistsAsync(parentId, name, cancellationToken))
            throw new DuplicateNameException("An entry with this name already exists in the folder.");

        if (type == EntryType.Folder)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                ParentId = parentId,
                Name = name,
                Path = BuildFolderPath(parent.Path, name)
            };
            await repository.AddFolderAsync(folder, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return new EntryDto(folder.Id, folder.Name, "folder");
        }

        var file = new File { Id = Guid.NewGuid(), ParentId = parentId, Name = name };
        await repository.AddFileAsync(file, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new EntryDto(file.Id, file.Name, "file");
    }

    public async Task DeleteFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        var folder = await repository.GetFolderAsync(id, cancellationToken)
                     ?? throw new EntryNotFoundException("Folder not found.");
        if (folder.ParentId is null) throw new CannotDeleteRootException("The root folder cannot be deleted.");

        await repository.DeleteFolderAsync(folder, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(Guid id, CancellationToken cancellationToken)
    {
        await repository.DeleteFileAsync(id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<SearchResultDto>> SearchAsync(string prefix, Guid? folderId, int limit,
        CancellationToken cancellationToken)
    {
        var normalized = NameValidator.Normalize(prefix);
        if (limit is < 1 or > 10)
            throw new DomainException("INVALID_LIMIT", "Limit must be between 1 and 10.");

        return repository.SearchAsync(normalized, folderId, limit, cancellationToken);
    }

    private static string BuildFolderPath(string parentPath, string name)
    {
        var full = $"{parentPath}/{name}";
        return full.Length <= NameValidator.MaxLength ? full : $"...{full[3..]}";
    }
}