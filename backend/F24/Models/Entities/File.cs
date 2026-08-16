namespace F24.Models.Entities;

public sealed class File
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
}