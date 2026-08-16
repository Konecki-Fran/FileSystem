namespace F24.Models.DTOs;

public sealed record SearchResultDto(Guid Id, string Name, string Path, Guid ParentId);