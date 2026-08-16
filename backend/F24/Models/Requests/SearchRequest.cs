using F24.Models.Validation;

namespace F24.Models.Requests;

public sealed class SearchRequest
{
    [SearchPrefix] public string Prefix { get; init; } = string.Empty;
}