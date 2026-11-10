using LessonOrganizer.Infrastructure.Identity;

namespace LessonOrganizer.Infrastructure.Tests.Identity;

public class ApplicationUserTests
{
    [Fact]
    public void New_WithoutId_GetsVersion7Id() => Assert.Equal(7, new ApplicationUser().Id.Version);

    [Fact]
    public void New_TwoUsers_GetDifferentIds() => Assert.NotEqual(new ApplicationUser().Id, new ApplicationUser().Id);
}
