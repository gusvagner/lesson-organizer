using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Domain.Tests;

public class LayerDependencyTests
{
    [Fact]
    public void DomainAssembly_ReferencesNoProjectOrFramework()
    {
        var forbidden = typeof(Result).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? "")
            .Where(name => name.StartsWith("LessonOrganizer.", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.Empty(forbidden);
    }
}
