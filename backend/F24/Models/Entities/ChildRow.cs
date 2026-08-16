namespace F24.Models.Entities;

internal sealed class ChildRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string SortName { get; set; } = string.Empty;
}