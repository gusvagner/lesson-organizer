namespace LessonOrganizer.Application.LessonPlans;

public sealed record LessonPlanRequest(Guid? StudentId, string? Title, string? Description, DateOnly? LessonDate);
