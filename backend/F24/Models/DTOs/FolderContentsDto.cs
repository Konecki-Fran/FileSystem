namespace F24.Models.DTOs;

public sealed record FolderContentsDto(Guid Id, string Name, Guid? ParentId, string Path, IReadOnlyList<EntryDto> Children);
