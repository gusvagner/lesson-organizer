using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Tests.Fakes;

public sealed class InMemoryStudentRepository(InMemoryData data) : IStudentRepository
{
    public Task AddAsync(Student student, CancellationToken ct)
    {
        data.Students.Add(student);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Student>> ListByTeacherAsync(Guid teacherId, string? search, CancellationToken ct)
    {
        IReadOnlyList<Student> students = data.Students
            .Where(student => student.TeacherId == teacherId)
            .Where(student => string.IsNullOrWhiteSpace(search)
                || student.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(student => student.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(student => student.Id)
            .ToList();
        return Task.FromResult(students);
    }

    public Task<Student?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct) =>
        Task.FromResult(data.Students.SingleOrDefault(student => student.Id == id && student.TeacherId == teacherId));

    public Task UpdateAsync(Student student, CancellationToken ct)
    {
        if (!data.Students.Contains(student))
        {
            throw new InvalidOperationException($"Student {student.Id} does not exist. Expected an existing student.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryDeleteAsync(Guid id, Guid teacherId, CancellationToken ct)
    {
        var hasLessonPlans = data.LessonPlans.Any(plan => plan.StudentId == id);
        var removed = !hasLessonPlans
            && data.Students.RemoveAll(student => student.Id == id && student.TeacherId == teacherId) > 0;
        return Task.FromResult(removed);
    }
}
