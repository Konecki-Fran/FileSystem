using F24.Models.DTOs;
using F24.Models.Entities;
using F24.Models.Enums;
using F24.Models.Requests;
using F24.Repositories;
using F24.Services;
using F24.Services.Exceptions;
using File = F24.Models.Entities.File;
using Xunit;

namespace F24.Tests;

public sealed class FileSystemServiceTests
{
    private static readonly Guid RootId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private static readonly Guid DocumentsId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetFolder_returns_folder_and_its_children()
    {
        var repository = CreateRepository();
        var service = new FileSystemService(repository);

        var result = await service.GetFolderAsync(RootId, CancellationToken.None);

        Assert.Equal("home", result.Name);
        Assert.Null(result.ParentId);
        Assert.Contains(result.Children, entry => entry.Name == "documents" && entry.Type == "folder");
    }

    [Fact]
    public async Task GetFolder_returns_not_found_for_unknown_folder()
    {
        var service = new FileSystemService(CreateRepository());

        await Assert.ThrowsAsync<EntryNotFoundException>(() => service.GetFolderAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData(EntryType.Folder, "folder")]
    [InlineData(EntryType.File, "file")]
    public async Task Create_adds_a_file_or_folder(EntryType type, string expectedType)
    {
        var repository = CreateRepository();
        var service = new FileSystemService(repository);

        var result = await service.CreateAsync(RootId, new CreateEntryRequest { Name = "  new-entry  ", Type = type },
            CancellationToken.None);

        Assert.Equal("new-entry", result.Name);
        Assert.Equal(expectedType, result.Type);
        Assert.True(await repository.EntryNameExistsAsync(RootId, "NEW-ENTRY", CancellationToken.None));
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task Create_returns_not_found_for_unknown_parent()
    {
        var service = new FileSystemService(CreateRepository());

        await Assert.ThrowsAsync<EntryNotFoundException>(() => service.CreateAsync(Guid.NewGuid(),
            new CreateEntryRequest { Name = "new-file", Type = EntryType.File }, CancellationToken.None));
    }

    [Fact]
    public async Task Create_rejects_duplicate_names_case_insensitively_across_entry_types()
    {
        var repository = CreateRepository();
        repository.AddExistingFile(RootId, "Report.txt");
        var service = new FileSystemService(repository);

        await Assert.ThrowsAsync<DuplicateNameException>(() => service.CreateAsync(RootId,
            new CreateEntryRequest { Name = "report.TXT", Type = EntryType.Folder }, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("folder/name")]
    [InlineData("folder\\name")]
    public async Task Create_rejects_invalid_names(string name)
    {
        var service = new FileSystemService(CreateRepository());

        await Assert.ThrowsAsync<InvalidNameException>(() => service.CreateAsync(RootId,
            new CreateEntryRequest { Name = name, Type = EntryType.File }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteFile_removes_an_existing_file_and_rejects_a_missing_one()
    {
        var repository = CreateRepository();
        var fileId = repository.AddExistingFile(RootId, "to-delete.txt");
        var service = new FileSystemService(repository);

        await service.DeleteFileAsync(fileId, CancellationToken.None);

        Assert.False(repository.HasFile(fileId));
        await Assert.ThrowsAsync<EntryNotFoundException>(() => service.DeleteFileAsync(fileId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteFolder_rejects_the_root_folder()
    {
        var service = new FileSystemService(CreateRepository());

        await Assert.ThrowsAsync<CannotDeleteRootException>(() => service.DeleteFolderAsync(RootId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteFolder_removes_the_folder_subtree()
    {
        var repository = CreateRepository();
        var projectId = repository.AddExistingFolder(DocumentsId, "projects");
        var fileId = repository.AddExistingFile(projectId, "notes.md");
        var service = new FileSystemService(repository);

        await service.DeleteFolderAsync(DocumentsId, CancellationToken.None);

        Assert.False(repository.HasFolder(DocumentsId));
        Assert.False(repository.HasFolder(projectId));
        Assert.False(repository.HasFile(fileId));
    }

    [Fact]
    public async Task Search_uses_normalized_prefix_and_folder_scope()
    {
        var repository = CreateRepository();
        repository.SearchResults = [new SearchResultDto(Guid.NewGuid(), "Readme.txt", "home/Readme.txt", RootId)];
        var service = new FileSystemService(repository);

        var result = await service.SearchAsync("  READ  ", DocumentsId, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("READ", repository.LastSearchPrefix);
        Assert.Equal(DocumentsId, repository.LastSearchFolderId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task Search_rejects_limits_outside_one_to_ten(int limit)
    {
        var service = new FileSystemService(CreateRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.SearchAsync("read", null, limit, CancellationToken.None));

        Assert.Equal("INVALID_LIMIT", exception.Code);
    }

    private static FakeFileSystemRepository CreateRepository()
    {
        var repository = new FakeFileSystemRepository();
        repository.AddRoot(RootId, "home");
        repository.AddExistingFolder(RootId, "documents", DocumentsId);
        return repository;
    }

    private sealed class FakeFileSystemRepository : IFileSystemRepository
    {
        private readonly Dictionary<Guid, Folder> folders = [];
        private readonly Dictionary<Guid, File> files = [];

        public int SaveChangesCount { get; private set; }
        public string? LastSearchPrefix { get; private set; }
        public Guid? LastSearchFolderId { get; private set; }
        public IReadOnlyList<SearchResultDto> SearchResults { get; set; } = [];

        public void AddRoot(Guid id, string name) => folders[id] = new Folder { Id = id, Name = name, Path = name };

        public Guid AddExistingFolder(Guid parentId, string name, Guid? id = null)
        {
            var folderId = id ?? Guid.NewGuid();
            var parentPath = folders[parentId].Path;
            folders[folderId] = new Folder { Id = folderId, ParentId = parentId, Name = name, Path = $"{parentPath}/{name}" };
            return folderId;
        }

        public Guid AddExistingFile(Guid parentId, string name)
        {
            var id = Guid.NewGuid();
            files[id] = new File { Id = id, ParentId = parentId, Name = name };
            return id;
        }

        public bool HasFolder(Guid id) => folders.ContainsKey(id);
        public bool HasFile(Guid id) => files.ContainsKey(id);

        public Task<Folder?> GetFolderAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(folders.GetValueOrDefault(id));

        public Task<IReadOnlyList<EntryDto>> GetChildrenAsync(Guid folderId, CancellationToken cancellationToken)
        {
            var entries = folders.Values.Where(folder => folder.ParentId == folderId)
                .Select(folder => new EntryDto(folder.Id, folder.Name, "folder"))
                .Concat(files.Values.Where(file => file.ParentId == folderId)
                    .Select(file => new EntryDto(file.Id, file.Name, "file")))
                .ToList();
            return Task.FromResult<IReadOnlyList<EntryDto>>(entries);
        }

        public Task<bool> EntryNameExistsAsync(Guid parentId, string name, CancellationToken cancellationToken) =>
            Task.FromResult(folders.Values.Any(folder => folder.ParentId == parentId &&
                                                          string.Equals(folder.Name, name, StringComparison.OrdinalIgnoreCase)) ||
                            files.Values.Any(file => file.ParentId == parentId &&
                                                     string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task AddFolderAsync(Folder folder, CancellationToken cancellationToken)
        {
            folders.Add(folder.Id, folder);
            return Task.CompletedTask;
        }

        public Task AddFileAsync(File file, CancellationToken cancellationToken)
        {
            files.Add(file.Id, file);
            return Task.CompletedTask;
        }

        public Task DeleteFolderAsync(Folder folder, CancellationToken cancellationToken)
        {
            var idsToDelete = new HashSet<Guid> { folder.Id };
            var pending = new Queue<Guid>([folder.Id]);
            while (pending.TryDequeue(out var parentId))
            {
                foreach (var childId in folders.Values.Where(child => child.ParentId == parentId).Select(child => child.Id).ToList())
                {
                    idsToDelete.Add(childId);
                    pending.Enqueue(childId);
                }
            }

            foreach (var fileId in files.Values.Where(file => idsToDelete.Contains(file.ParentId)).Select(file => file.Id).ToList())
                files.Remove(fileId);
            foreach (var folderId in idsToDelete)
                folders.Remove(folderId);
            return Task.CompletedTask;
        }

        public Task DeleteFileAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!files.Remove(id)) throw new EntryNotFoundException("File not found.");
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SearchResultDto>> SearchAsync(string prefix, Guid? folderId, int limit,
            CancellationToken cancellationToken)
        {
            LastSearchPrefix = prefix;
            LastSearchFolderId = folderId;
            return Task.FromResult(SearchResults);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }
}
