using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Application.LessonPlans;

public sealed record LessonPlanFilter(Guid? StudentId, LessonPlanStatus? Status);
