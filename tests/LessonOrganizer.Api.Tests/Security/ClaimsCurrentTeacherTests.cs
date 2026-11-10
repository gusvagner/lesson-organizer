using System.Security.Claims;
using LessonOrganizer.Api.Security;
using Microsoft.AspNetCore.Http;

namespace LessonOrganizer.Api.Tests.Security;

public class ClaimsCurrentTeacherTests
{
    [Fact]
    public void Id_ValidClaim_ReturnsGuid()
    {
        var id = Guid.CreateVersion7();
        Assert.Equal(id, TeacherWith(new Claim(ClaimTypes.NameIdentifier, id.ToString())).Id);
    }

    [Fact]
    public void Id_MissingClaim_ThrowsNamingExpectedShape()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TeacherWith().Id);
        Assert.Contains("Expected a Guid user id", ex.Message);
    }

    [Fact]
    public void Id_NotAGuid_ThrowsNamingValue()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TeacherWith(new Claim(ClaimTypes.NameIdentifier, "abc")).Id);
        Assert.Contains("'abc'", ex.Message);
    }

    private static ClaimsCurrentTeacher TeacherWith(params Claim[] claims)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
        return new ClaimsCurrentTeacher(new HttpContextAccessor { HttpContext = context });
    }
}
