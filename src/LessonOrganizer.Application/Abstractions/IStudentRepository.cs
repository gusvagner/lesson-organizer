using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Abstractions;

public interface IStudentRepository
{
    Task AddAsync(Student student, CancellationToken ct);

    Task<IReadOnlyList<Student>> ListByTeacherAsync(Guid teacherId, string? search, CancellationToken ct);

    Task<Student?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct);

    Task UpdateAsync(Student student, CancellationToken ct);

    Task<bool> TryDeleteAsync(Guid id, Guid teacherId, CancellationToken ct);
}
