namespace F24.Models.Entities;

internal sealed class SearchRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
}