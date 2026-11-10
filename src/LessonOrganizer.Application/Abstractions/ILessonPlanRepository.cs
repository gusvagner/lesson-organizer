using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Application.Abstractions;

public interface ILessonPlanRepository
{
    Task AddAsync(LessonPlan plan, CancellationToken ct);

    Task<IReadOnlyList<LessonPlan>> ListByTeacherAsync(Guid teacherId, LessonPlanFilter filter, CancellationToken ct);

    Task<LessonPlan?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct);

    Task<IReadOnlyDictionary<Guid, int>> CountByStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct);

    Task UpdateAsync(LessonPlan plan, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);
}
