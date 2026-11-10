using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Application.LessonPlans;

public sealed record ChangeLessonPlanStatusRequest(LessonPlanStatus? Status);
