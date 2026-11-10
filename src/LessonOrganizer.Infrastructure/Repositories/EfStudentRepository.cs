using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Repositories;

public sealed class EfStudentRepository(LessonOrganizerDbContext db) : IStudentRepository
{
    public async Task AddAsync(Student student, CancellationToken ct)
    {
        db.Students.Add(student);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Student>> ListByTeacherAsync(Guid teacherId, string? search, CancellationToken ct)
    {
        var query = db.Students.AsNoTracking().Where(student => student.TeacherId == teacherId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search.Trim());
            query = query.Where(student => EF.Functions.Like(student.Name, pattern, LikePattern.EscapeCharacter));
        }

        return await query.OrderBy(student => student.Name).ThenBy(student => student.Id).ToListAsync(ct);
    }

    public Task<Student?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct) =>
        db.Students.AsNoTracking().SingleOrDefaultAsync(student => student.Id == id && student.TeacherId == teacherId, ct);

    public async Task UpdateAsync(Student student, CancellationToken ct)
    {
        db.Students.Update(student);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                $"Student {student.Id} does not exist. Expected an existing student id.", ex);
        }
    }

    public async Task<bool> TryDeleteAsync(Guid id, Guid teacherId, CancellationToken ct)
    {
        if (await db.LessonPlans.AnyAsync(plan => plan.StudentId == id, ct))
        {
            return false;
        }

        try
        {
            var deleted = await db.Students
                .Where(student => student.Id == id && student.TeacherId == teacherId)
                .ExecuteDeleteAsync(ct);
            return deleted > 0;
        }
        catch (Exception ex) when (SqlErrors.IsForeignKeyViolation(ex))
        {
            return false;
        }
    }
}
