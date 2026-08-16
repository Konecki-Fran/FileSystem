using System.ComponentModel.DataAnnotations;

namespace F24.Models.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FileSystemNameAttribute()
    : ValidationAttribute("Name must be 1-255 characters and must not contain '/' or '\\'.")
{
    public override bool IsValid(object? value)
    {
        if (value is not string name) return false;
        var normalized = name.Trim();
        return normalized.Length is > 0 and <= 255 && !normalized.Contains('/') && !normalized.Contains('\\');
    }
}