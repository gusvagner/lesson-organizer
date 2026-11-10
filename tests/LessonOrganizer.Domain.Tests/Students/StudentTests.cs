using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Domain.Tests.Students;

public class StudentTests
{
    private static readonly Guid TeacherId = Guid.CreateVersion7();

    private static Student Ana() => Student.Create(TeacherId, "Ana", null, EnglishLevel.Intermediate, null).Value;

    [Fact]
    public void Update_InvalidEmail_LeavesStudentUnchanged()
    {
        var student = Ana();
        var result = student.Update("Ana Maria", "not-an-email", EnglishLevel.Advanced, "notes");
        Assert.False(result.IsSuccess);
        Assert.Equal("Ana", student.Name);
        Assert.Equal(EnglishLevel.Intermediate, student.EnglishLevel);
        Assert.Null(student.Notes);
    }

    [Fact]
    public void Create_NumericLevelNotDefined_ReturnsEnglishLevelError()
    {
        var result = Student.Create(TeacherId, "Ana", null, (EnglishLevel)99, null);
        var error = Assert.Single(result.Errors);
        Assert.Equal("englishLevel", error.Field);
        Assert.Equal("English level must be one of: Beginner, Elementary, Intermediate, UpperIntermediate, Advanced.", error.Message);
    }

    [Fact]
    public void Create_ValidData_TrimsAndStoresNullForEmptyOptionals()
    {
        var student = Student.Create(TeacherId, " Ana ", "  ", EnglishLevel.Beginner, "").Value;
        Assert.Equal("Ana", student.Name);
        Assert.Null(student.Email);
        Assert.Null(student.Notes);
        Assert.Equal(TeacherId, student.TeacherId);
    }

    [Fact]
    public void Create_NameMissingAndEmailInvalid_ReturnsBothErrors()
    {
        var result = Student.Create(TeacherId, " ", "ana@", EnglishLevel.Beginner, null);
        Assert.Equal(["name", "email"], result.Errors.Select(error => error.Field));
    }

    [Fact]
    public void Create_NotesOver2000_ReturnsNotesError()
    {
        var result = Student.Create(TeacherId, "Ana", null, EnglishLevel.Beginner, new string('n', 2001));
        Assert.Equal(Error.Validation("notes", "Notes must be at most 2000 characters."), Assert.Single(result.Errors));
    }

    [Fact]
    public void Create_EmailOver254_ReturnsEmailError()
    {
        var email = new string('a', 243) + "@example.com";
        var result = Student.Create(TeacherId, "Ana", email, EnglishLevel.Beginner, null);
        Assert.Equal(Error.Validation("email", "Email must be at most 254 characters."), Assert.Single(result.Errors));
    }

    [Fact]
    public void Create_GeneratesVersion7Id() => Assert.Equal(7, Ana().Id.Version);

    [Fact]
    public void Update_ValidData_ChangesAllFields()
    {
        var student = Ana();
        var result = student.Update(" Ana Maria ", "ana@example.com", EnglishLevel.Advanced, "Likes music");
        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Maria", student.Name);
        Assert.Equal("ana@example.com", student.Email);
        Assert.Equal(EnglishLevel.Advanced, student.EnglishLevel);
        Assert.Equal("Likes music", student.Notes);
    }
}
