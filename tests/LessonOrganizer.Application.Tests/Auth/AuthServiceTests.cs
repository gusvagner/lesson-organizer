using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Testing.Fakes;

namespace LessonOrganizer.Application.Tests.Auth;

public class AuthServiceTests
{
    private const string Password = "Demo1234!";
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly InMemoryUserAccounts _accounts = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_accounts);
    }

    [Fact]
    public async Task Login_LockedOut_ReturnsSameErrorAsWrongPassword()
    {
        await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct);
        var wrong = await _service.LoginAsync(new LoginRequest("maria@example.com", "wrong-pass"), Ct);
        for (var i = 0; i < 4; i++)
        {
            await _service.LoginAsync(new LoginRequest("maria@example.com", "wrong-pass"), Ct);
        }

        var locked = await _service.LoginAsync(new LoginRequest("maria@example.com", Password), Ct);
        Assert.Equal(Error.Unauthorized("Invalid email or password."), Assert.Single(wrong.Errors));
        Assert.Equal(wrong.Errors, locked.Errors);
    }

    [Fact]
    public async Task Register_PasswordOf7Chars_ReturnsPasswordErrorAndCreatesNothing()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("Maria", "maria@example.com", "1234567"), Ct);
        Assert.Equal(Error.Validation("password", "Password must be at least 8 characters."), Assert.Single(result.Errors));
        Assert.Empty(_accounts.Users);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsUser()
    {
        var result = await _service.RegisterAsync(new RegisterRequest(" Maria Silva ", " maria@example.com ", Password), Ct);
        Assert.Equal(("Maria Silva", "maria@example.com"), (result.Value.Name, result.Value.Email));
        Assert.Single(_accounts.Users);
    }

    [Fact]
    public async Task Register_MissingNameInvalidEmailMissingPassword_ReturnsAllErrors()
    {
        var result = await _service.RegisterAsync(new RegisterRequest(" ", "maria@", null), Ct);
        Assert.Equal(["name", "email", "password"], result.Errors.Select(error => error.Field));
        Assert.Equal("Password is required.", result.Errors[2].Message);
        Assert.Empty(_accounts.Users);
    }

    [Fact]
    public async Task Register_PasswordOf129Chars_ReturnsMaxLengthError()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("Maria", "maria@example.com", new string('p', 129)), Ct);
        Assert.Equal(Error.Validation("password", "Password must be at most 128 characters."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Register_PasswordOf8Spaces_IsAccepted()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("Maria", "maria@example.com", "        "), Ct);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Register_ExistingEmailOtherCase_ReturnsConflict()
    {
        await _service.RegisterAsync(new RegisterRequest("Maria", "maria@example.com", Password), Ct);
        var result = await _service.RegisterAsync(new RegisterRequest("Other", "MARIA@example.com", Password), Ct);
        Assert.Equal(Error.Conflict("An account with this email already exists."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsUser()
    {
        await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct);
        var result = await _service.LoginAsync(new LoginRequest(" maria@example.com ", Password), Ct);
        Assert.Equal("Maria", result.Value.Name);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var result = await _service.LoginAsync(new LoginRequest("nobody@example.com", Password), Ct);
        Assert.Equal(Error.Unauthorized("Invalid email or password."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Login_BlankEmailAndPassword_ReturnsBothRequiredErrors()
    {
        var result = await _service.LoginAsync(new LoginRequest(" ", ""), Ct);
        Assert.Equal(
            [Error.Validation("email", "Email is required."), Error.Validation("password", "Password is required.")],
            result.Errors);
    }

    [Fact]
    public async Task Get_ExistingUser_ReturnsUser()
    {
        var created = await _accounts.CreateAsync("Maria", "maria@example.com", Password, Ct);
        var result = await _service.GetAsync(created.Value.Id, Ct);
        Assert.Equal("maria@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Get_MissingUser_ReturnsNotFound()
    {
        var result = await _service.GetAsync(Guid.CreateVersion7(), Ct);
        Assert.Equal(Error.NotFound("User not found."), Assert.Single(result.Errors));
    }
}
