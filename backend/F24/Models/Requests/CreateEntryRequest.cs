using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using F24.Models.Enums;
using F24.Models.Validation;

namespace F24.Models.Requests;

public sealed class CreateEntryRequest
{
    [FileSystemName] public string Name { get; init; } = string.Empty;

    [JsonRequired]
    [EnumDataType(typeof(EntryType))]
    public EntryType Type { get; init; }
}