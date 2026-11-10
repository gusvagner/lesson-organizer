using LessonOrganizer.Infrastructure.Repositories;

namespace LessonOrganizer.Infrastructure.Tests.Repositories;

public class LikePatternTests
{
    [Fact]
    public void Contains_EscapesWildcards() =>
        Assert.Equal("%50\\%\\_\\[x]\\\\%", LikePattern.Contains("50%_[x]\\"));

    [Fact]
    public void Contains_PlainText_WrapsInPercent() => Assert.Equal("%ana%", LikePattern.Contains("ana"));
}
