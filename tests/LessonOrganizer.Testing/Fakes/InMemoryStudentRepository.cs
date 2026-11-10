using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Testing.Fakes;

public sealed class InMemoryStudentRepository(InMemoryData data) : IStudentRepository
{
    public Task AddAsync(Student student, CancellationToken ct)
    {
        data.Students.Add(EntityCopy.Of(student));
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
            .Select(EntityCopy.Of)
            .ToList();
        return Task.FromResult(students);
    }

    public Task<Student?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct)
    {
        var stored = data.Students.SingleOrDefault(student => student.Id == id && student.TeacherId == teacherId);
        return Task.FromResult(stored is null ? null : EntityCopy.Of(stored));
    }

    public Task UpdateAsync(Student student, CancellationToken ct)
    {
        var index = data.Students.FindIndex(stored => stored.Id == student.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Student {student.Id} does not exist. Expected an existing student id.");
        }

        data.Students[index] = EntityCopy.Of(student);
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
