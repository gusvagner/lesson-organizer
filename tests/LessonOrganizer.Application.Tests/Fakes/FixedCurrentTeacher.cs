using LessonOrganizer.Application.Abstractions;

namespace LessonOrganizer.Application.Tests.Fakes;

public sealed class FixedCurrentTeacher(Guid id) : ICurrentTeacher
{
    public Guid Id { get; } = id;
}
