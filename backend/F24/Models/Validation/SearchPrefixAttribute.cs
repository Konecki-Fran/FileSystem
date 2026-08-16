using System.ComponentModel.DataAnnotations;

namespace F24.Models.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SearchPrefixAttribute() : ValidationAttribute("Search prefix must be 1-255 characters.")
{
    public override bool IsValid(object? value)
    {
        return value is string prefix && prefix.Trim().Length is > 0 and <= 255;
    }
}