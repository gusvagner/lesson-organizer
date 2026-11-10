namespace LessonOrganizer.Domain.Common;

public static class EmailField
{
    public const int MaxLength = 254;

    public static string? Optional(string? value, ICollection<Error> errors)
    {
        var errorCountBefore = errors.Count;
        var email = TextField.Optional(value, "email", "Email", MaxLength, errors);
        return email is null ? null : Formatted(email, errors, errorCountBefore);
    }

    public static string? Required(string? value, ICollection<Error> errors)
    {
        var errorCountBefore = errors.Count;
        var email = TextField.Required(value, "email", "Email", MaxLength, errors);
        return email is null ? null : Formatted(email, errors, errorCountBefore);
    }

    private static string? Formatted(string email, ICollection<Error> errors, int errorCountBefore)
    {
        if (errors.Count > errorCountBefore || EmailFormat.IsValid(email))
        {
            return email;
        }

        errors.Add(Error.Validation("email", "Email is not a valid email address."));
        return null;
    }
}
