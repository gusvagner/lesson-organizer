using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Application.Tests.Fakes;

public sealed class InMemoryLessonPlanRepository(InMemoryData data) : ILessonPlanRepository
{
    public Task AddAsync(LessonPlan plan, CancellationToken ct)
    {
        EnsureStudentExists(plan);
        data.LessonPlans.Add(EntityCopy.Of(plan));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LessonPlan>> ListByTeacherAsync(Guid teacherId, LessonPlanFilter filter, CancellationToken ct)
    {
        IReadOnlyList<LessonPlan> plans = data.LessonPlans
            .Where(plan => IsOwnedBy(plan, teacherId))
            .Where(plan => filter.StudentId is null || plan.StudentId == filter.StudentId)
            .Where(plan => filter.Status is null || plan.Status == filter.Status)
            .OrderBy(plan => plan.LessonDate)
            .ThenBy(plan => plan.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(plan => plan.Id)
            .Select(EntityCopy.Of)
            .ToList();
        return Task.FromResult(plans);
    }

    public Task<LessonPlan?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct)
    {
        var stored = data.LessonPlans.SingleOrDefault(plan => plan.Id == id && IsOwnedBy(plan, teacherId));
        return Task.FromResult(stored is null ? null : EntityCopy.Of(stored));
    }

    public Task<IReadOnlyDictionary<Guid, int>> CountByStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct)
    {
        IReadOnlyDictionary<Guid, int> counts = studentIds.Distinct().ToDictionary(
            studentId => studentId,
            studentId => data.LessonPlans.Count(plan => plan.StudentId == studentId));
        return Task.FromResult(counts);
    }

    public Task UpdateAsync(LessonPlan plan, CancellationToken ct)
    {
        EnsureStudentExists(plan);
        var index = data.LessonPlans.FindIndex(stored => stored.Id == plan.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Lesson plan {plan.Id} does not exist. Expected an existing lesson plan id.");
        }

        data.LessonPlans[index] = EntityCopy.Of(plan);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        data.LessonPlans.RemoveAll(plan => plan.Id == id);
        return Task.CompletedTask;
    }

    private bool IsOwnedBy(LessonPlan plan, Guid teacherId) =>
        data.Students.Any(student => student.Id == plan.StudentId && student.TeacherId == teacherId);

    private void EnsureStudentExists(LessonPlan plan)
    {
        if (data.Students.All(student => student.Id != plan.StudentId))
        {
            throw new InvalidOperationException(
                $"Lesson plan {plan.Id} references student {plan.StudentId}, which does not exist. Expected an existing student id.");
        }
    }
}
