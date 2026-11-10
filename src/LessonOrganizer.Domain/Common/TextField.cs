namespace LessonOrganizer.Domain.Common;

public static class TextField
{
    public static string? Required(string? value, string field, string label, int maxLength, ICollection<Error> errors)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            errors.Add(Error.Validation(field, $"{label} is required."));
            return null;
        }

        return WithinMaxLength(trimmed, field, label, maxLength, errors);
    }

    public static string? Optional(string? value, string field, string label, int maxLength, ICollection<Error> errors)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : WithinMaxLength(trimmed, field, label, maxLength, errors);
    }

    private static string? WithinMaxLength(string value, string field, string label, int maxLength, ICollection<Error> errors)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        errors.Add(Error.Validation(field, $"{label} must be at most {maxLength} characters."));
        return null;
    }
}
