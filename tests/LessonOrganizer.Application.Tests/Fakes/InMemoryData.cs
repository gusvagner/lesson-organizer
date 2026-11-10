using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Tests.Fakes;

public sealed class InMemoryData
{
    public List<Student> Students { get; } = [];

    public List<LessonPlan> LessonPlans { get; } = [];
}
