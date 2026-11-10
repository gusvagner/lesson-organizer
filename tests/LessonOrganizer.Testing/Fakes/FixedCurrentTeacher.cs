using LessonOrganizer.Application.Abstractions;

namespace LessonOrganizer.Testing.Fakes;

public sealed class FixedCurrentTeacher(Guid id) : ICurrentTeacher
{
    public Guid Id { get; } = id;
}
