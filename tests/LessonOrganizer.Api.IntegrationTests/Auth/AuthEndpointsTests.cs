using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Api.IntegrationTests.Auth;

public sealed class AuthEndpointsTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const string Password = ApiClient.DemoPassword;
    private const string XsrfProblemType = "urn:lesson-organizer:problem:invalid-xsrf-token";

    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _factory = new ApiFactory(sql);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Login_Valid_SetsHttpOnlySecureStrictCookie()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        var response = await client.LoginAsync("maria@example.com", Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithoutXsrfToken_IsRejected()
    {
        var http = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await http.PostAsJsonAsync("/api/auth/register", new { name = "Maria", email = "maria@example.com", password = Password }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(XsrfProblemType, (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("type").GetString());
        await using var context = _factory.NewDbContext();
        Assert.Equal(0, await context.Users.CountAsync(Ct));
    }

    [Fact]
    public async Task TokenFromBeforeLogin_IsRejectedAfterLoginWithXsrfType()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        var staleToken = client.Http.DefaultRequestHeaders.GetValues("X-XSRF-TOKEN").Single();
        await client.LoginAsync("maria@example.com", Password);
        client.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.Http.DefaultRequestHeaders.Add("X-XSRF-TOKEN", staleToken);
        var response = await client.Http.PostAsJsonAsync("/api/students", new { name = "Ana", englishLevel = "Beginner" }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(XsrfProblemType, (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("type").GetString());
    }

    [Fact]
    public async Task Register_Valid_Returns201ThenLoginWorks()
    {
        var client = await ApiClient.CreateAsync(_factory);
        var register = await client.RegisterAsync("Maria", "maria@example.com", Password);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync("maria@example.com", Password)).StatusCode);
    }

    [Fact]
    public async Task Register_DoesNotStartSession()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Http.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailOtherCase_Returns409()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        Assert.Equal(HttpStatusCode.Conflict, (await client.RegisterAsync("Other", "MARIA@example.com", Password)).StatusCode);
    }

    [Fact]
    public async Task Me_WithoutSession_Returns401NotRedirect()
    {
        var client = await ApiClient.CreateAsync(_factory);
        var response = await client.Http.GetAsync("/api/auth/me", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsUser()
    {
        var client = await ApiClient.LoggedInAsync(_factory, "maria@example.com");
        var me = await client.Http.GetFromJsonAsync<JsonElement>("/api/auth/me", Ct);
        Assert.Equal("maria@example.com", me.GetProperty("email").GetString());
        Assert.Equal("maria", me.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Me_AfterLogout_Returns401()
    {
        var client = await ApiClient.LoggedInAsync(_factory, "maria@example.com");
        Assert.Equal(HttpStatusCode.NoContent, (await client.LogoutAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Http.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericMessage()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        var response = await client.LoginAsync("maria@example.com", "wrong-pass");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Invalid email or password.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_FiveWrongThenRight_Returns401()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        for (var i = 0; i < 5; i++)
        {
            await client.LoginAsync("maria@example.com", "wrong-pass");
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync("maria@example.com", Password)).StatusCode);
    }

    [Fact]
    public async Task Login_EmailInOtherCase_Returns200()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync("MARIA@EXAMPLE.COM", Password)).StatusCode);
    }

    [Fact]
    public async Task StoredUser_HasHashNotPassword()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", "maria@example.com", Password);
        await using var context = _factory.NewDbContext();
        var hash = await context.Users.Select(user => user.PasswordHash).SingleAsync(Ct);
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.DoesNotContain(Password, hash, StringComparison.Ordinal);
    }
}
