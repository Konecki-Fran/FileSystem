using F24.Services.Exceptions;

namespace F24.Services;

public static class NameValidator
{
    public const int MaxLength = 255;

    public static string Normalize(string? name)
    {
        if (name is null) throw new InvalidNameException("Name is required.");

        var normalized = name.Trim();
        if (normalized.Length == 0) throw new InvalidNameException("Name must not be empty.");
        if (normalized.Length > MaxLength)
            throw new InvalidNameException($"Name must be at most {MaxLength} characters.");
        if (normalized.Contains('/') || normalized.Contains('\\'))
            throw new InvalidNameException("Name must not contain '/' or '\\'.");

        return normalized;
    }
}