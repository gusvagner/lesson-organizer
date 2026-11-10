using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Application.Tests.Fakes;

public sealed class InMemoryLessonPlanRepository(InMemoryData data) : ILessonPlanRepository
{
    public Task AddAsync(LessonPlan plan, CancellationToken ct)
    {
        EnsureStudentExists(plan);
        data.LessonPlans.Add(plan);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LessonPlan>> ListByTeacherAsync(Guid teacherId, LessonPlanFilter filter, CancellationToken ct)
    {
        var ownStudentIds = data.Students.Where(student => student.TeacherId == teacherId).Select(student => student.Id).ToHashSet();
        IReadOnlyList<LessonPlan> plans = data.LessonPlans
            .Where(plan => ownStudentIds.Contains(plan.StudentId))
            .Where(plan => filter.StudentId is null || plan.StudentId == filter.StudentId)
            .Where(plan => filter.Status is null || plan.Status == filter.Status)
            .OrderBy(plan => plan.LessonDate)
            .ThenBy(plan => plan.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(plan => plan.Id)
            .ToList();
        return Task.FromResult(plans);
    }

    public Task<LessonPlan?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct) =>
        Task.FromResult(data.LessonPlans.SingleOrDefault(plan => plan.Id == id && IsOwnedBy(plan, teacherId)));

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
