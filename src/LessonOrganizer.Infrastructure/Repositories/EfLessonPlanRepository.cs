using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Repositories;

public sealed class EfLessonPlanRepository(LessonOrganizerDbContext db) : ILessonPlanRepository
{
    public async Task AddAsync(LessonPlan plan, CancellationToken ct)
    {
        db.LessonPlans.Add(plan);
        await SaveAsync(plan, ct);
    }

    public async Task<IReadOnlyList<LessonPlan>> ListByTeacherAsync(Guid teacherId, LessonPlanFilter filter, CancellationToken ct)
    {
        var query = OwnedBy(teacherId);
        if (filter.StudentId is { } studentId)
        {
            query = query.Where(plan => plan.StudentId == studentId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(plan => plan.Status == status);
        }

        return await query
            .OrderBy(plan => plan.LessonDate)
            .ThenBy(plan => plan.Title)
            .ThenBy(plan => plan.Id)
            .ToListAsync(ct);
    }

    public Task<LessonPlan?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct) =>
        OwnedBy(teacherId).SingleOrDefaultAsync(plan => plan.Id == id, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> CountByStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct)
    {
        var counts = await db.LessonPlans
            .Where(plan => studentIds.Contains(plan.StudentId))
            .GroupBy(plan => plan.StudentId)
            .Select(group => new { StudentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.StudentId, row => row.Count, ct);
        return studentIds.Distinct().ToDictionary(studentId => studentId, studentId => counts.GetValueOrDefault(studentId));
    }

    public async Task UpdateAsync(LessonPlan plan, CancellationToken ct)
    {
        db.LessonPlans.Update(plan);
        try
        {
            await SaveAsync(plan, ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                $"Lesson plan {plan.Id} does not exist. Expected an existing lesson plan id.", ex);
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken ct) =>
        db.LessonPlans.Where(plan => plan.Id == id).ExecuteDeleteAsync(ct);

    private IQueryable<LessonPlan> OwnedBy(Guid teacherId) =>
        db.LessonPlans.AsNoTracking()
            .Where(plan => db.Students.Any(student => student.Id == plan.StudentId && student.TeacherId == teacherId));

    private async Task SaveAsync(LessonPlan plan, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (SqlErrors.IsForeignKeyViolation(ex))
        {
            throw new InvalidOperationException(
                $"Lesson plan {plan.Id} references student {plan.StudentId}, which does not exist. Expected an existing student id.",
                ex);
        }
    }
}
