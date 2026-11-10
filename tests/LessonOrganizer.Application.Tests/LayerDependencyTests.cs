using LessonOrganizer.Application.Students;

namespace LessonOrganizer.Application.Tests;

public class LayerDependencyTests
{
    [Fact]
    public void ApplicationAssembly_ReferencesOnlyDomain()
    {
        var names = typeof(StudentService).Assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? "").ToList();
        Assert.Equal(["LessonOrganizer.Domain"], names.Where(name => name.StartsWith("LessonOrganizer.", StringComparison.Ordinal)));
        Assert.DoesNotContain(names, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
