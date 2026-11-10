using LessonOrganizer.Api.Controllers;
using LessonOrganizer.Api.Tests.Fakes;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Testing.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Tests.Controllers;

public class AuthControllerTests
{
    private const string Password = "Demo1234!";
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly InMemoryUserAccounts _accounts = new();
    private readonly RecordingAuthSession _session = new();

    [Fact]
    public async Task Login_WrongPassword_Returns401AndStartsNoSession()
    {
        await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct);
        var result = await Controller().Login(new LoginRequest("maria@example.com", "wrong-pass"), Ct);
        Assert.Equal(401, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Null(_session.SignedInUserId);
    }

    [Fact]
    public async Task Login_Valid_Returns200AndSignsIn()
    {
        var maria = (await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct)).Value;
        var result = await Controller().Login(new LoginRequest("maria@example.com", Password), Ct);
        Assert.Equal(maria, Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(maria.Id, _session.SignedInUserId);
    }

    [Fact]
    public async Task Register_Valid_Returns201AndStartsNoSession()
    {
        var result = await Controller().Register(new RegisterRequest("Maria", "maria@example.com", Password), Ct);
        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("maria@example.com", Assert.IsType<UserResponse>(created.Value).Email);
        Assert.Null(_session.SignedInUserId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct);
        var result = await Controller().Register(new RegisterRequest("Other", "maria@example.com", Password), Ct);
        Assert.Equal(409, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var result = await Controller().Register(new RegisterRequest("Maria", "maria@example.com", "short"), Ct);
        Assert.Equal(400, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Logout_SignsOutAndReturns204()
    {
        var result = await Controller().Logout();
        Assert.IsType<NoContentResult>(result);
        Assert.True(_session.SignedOut);
    }

    [Fact]
    public async Task Me_ExistingUser_Returns200()
    {
        var maria = (await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct)).Value;
        var result = await Controller(maria.Id).Me(Ct);
        Assert.Equal(maria, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Me_UserGone_Returns401()
    {
        var result = await Controller(Guid.CreateVersion7()).Me(Ct);
        Assert.Equal(401, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    private AuthController Controller(Guid? currentUserId = null) =>
        new(new AuthService(_accounts), _session, new FixedCurrentTeacher(currentUserId ?? Guid.CreateVersion7()));
}
