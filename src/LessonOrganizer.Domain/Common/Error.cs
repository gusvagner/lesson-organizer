namespace LessonOrganizer.Domain.Common;

public sealed record Error(ErrorKind Kind, string Message, string? Field = null)
{
    public static Error Validation(string field, string message) => new(ErrorKind.Validation, message, field);

    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);

    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);

    public static Error Unauthorized(string message) => new(ErrorKind.Unauthorized, message);
}
