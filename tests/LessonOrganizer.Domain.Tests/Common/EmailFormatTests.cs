using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Domain.Tests.Common;

public class EmailFormatTests
{
    [Theory]
    [InlineData("Ana <ana@example.com>")]
    [InlineData("ana")]
    [InlineData("ana@")]
    public void IsValid_NotPlainAddress_ReturnsFalse(string email) => Assert.False(EmailFormat.IsValid(email));

    [Fact]
    public void IsValid_PlainAddress_ReturnsTrue() => Assert.True(EmailFormat.IsValid("ana@example.com"));
}
