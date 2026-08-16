namespace F24.Services.Exceptions;

public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class InvalidNameException(string message) : DomainException("INVALID_NAME", message);

public sealed class EntryNotFoundException(string message) : DomainException("NOT_FOUND", message);

public sealed class DuplicateNameException(string message) : DomainException("NAME_ALREADY_EXISTS", message);

public sealed class CannotDeleteRootException(string message) : DomainException("CANNOT_DELETE_ROOT", message);