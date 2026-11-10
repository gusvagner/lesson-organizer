using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Domain.Students;

public sealed class Student
{
    public const int NameMaxLength = 150;
    public const int NotesMaxLength = 2000;

    private Student()
    {
        Name = "";
    }

    public Guid Id { get; private set; }

    public Guid TeacherId { get; private set; }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public EnglishLevel EnglishLevel { get; private set; }

    public string? Notes { get; private set; }

    public static Result<Student> Create(Guid teacherId, string? name, string? email, EnglishLevel englishLevel, string? notes)
    {
        var student = new Student { Id = Guid.CreateVersion7(), TeacherId = teacherId };
        var result = student.Update(name, email, englishLevel, notes);
        return result.IsSuccess ? Result<Student>.Success(student) : Result<Student>.Failure(result.Errors);
    }

    public Result Update(string? name, string? email, EnglishLevel englishLevel, string? notes)
    {
        var errors = new List<Error>();
        var validName = TextField.Required(name, "name", "Name", NameMaxLength, errors);
        var validEmail = EmailField.Optional(email, errors);
        CheckEnglishLevel(englishLevel, errors);
        var validNotes = TextField.Optional(notes, "notes", "Notes", NotesMaxLength, errors);
        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        Name = validName!;
        Email = validEmail;
        EnglishLevel = englishLevel;
        Notes = validNotes;
        return Result.Success();
    }

    private static void CheckEnglishLevel(EnglishLevel englishLevel, List<Error> errors)
    {
        if (!Enum.IsDefined(englishLevel))
        {
            errors.Add(Error.Validation(
                "englishLevel",
                $"English level must be one of: {string.Join(", ", Enum.GetNames<EnglishLevel>())}."));
        }
    }
}
