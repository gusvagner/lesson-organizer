using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Application.LessonPlans;

public sealed record LessonPlanResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string Title,
    string? Description,
    DateOnly LessonDate,
    LessonPlanStatus Status)
{
    public static LessonPlanResponse From(LessonPlan plan, string studentName) =>
        new(plan.Id, plan.StudentId, studentName, plan.Title, plan.Description, plan.LessonDate, plan.Status);
}
