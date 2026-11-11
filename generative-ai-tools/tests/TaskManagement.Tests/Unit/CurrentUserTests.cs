using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManagement.Api.Auth;

namespace TaskManagement.Tests.Unit;

public sealed class CurrentUserTests
{
    [Fact]
    public void UserId_SubjectClaimIsGuid_ReturnsIt()
    {
        var userId = Guid.NewGuid();

        var currentUser = new CurrentUser(AccessorWithClaims(new Claim("sub", userId.ToString())));

        Assert.Equal(userId, currentUser.UserId);
    }

    [Fact]
    public void UserId_OnlyNameIdentifierClaim_IgnoresItAndThrows()
    {
        var currentUser = new CurrentUser(AccessorWithClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())));

        var exception = Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
        Assert.Contains("sub", exception.Message);
    }

    [Fact]
    public void UserId_SubjectNotAGuid_ThrowsNamingTheValue()
    {
        var currentUser = new CurrentUser(AccessorWithClaims(new Claim("sub", "not-a-guid")));

        var exception = Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
        Assert.Contains("not-a-guid", exception.Message);
    }

    [Fact]
    public void UserId_NoHttpContext_Throws()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());

        Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
    }

    private static HttpContextAccessor AccessorWithClaims(params Claim[] claims) => new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
    };
}
