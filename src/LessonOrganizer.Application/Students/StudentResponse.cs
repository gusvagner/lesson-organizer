using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Students;

public sealed record StudentResponse(Guid Id, string Name, string? Email, EnglishLevel EnglishLevel, string? Notes, int LessonPlanCount)
{
    public static StudentResponse From(Student student, int lessonPlanCount) =>
        new(student.Id, student.Name, student.Email, student.EnglishLevel, student.Notes, lessonPlanCount);
}
