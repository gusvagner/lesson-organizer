using System.Reflection;

namespace LessonOrganizer.Application.Tests.Fakes;

public static class EntityCopy
{
    private static readonly MethodInfo CloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static T Of<T>(T entity)
        where T : class => (T)CloneMethod.Invoke(entity, null)!;
}
