using System.Net;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LessonOrganizer.Api.IntegrationTests.Auth;

public sealed class DataProtectionTests(SqlServerFixture sql) : IDisposable
{
    private const string AuthCookieName = ".AspNetCore.Identity.Application";

    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"lo-keys-{Guid.NewGuid():N}");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        if (Directory.Exists(_keysPath))
        {
            Directory.Delete(_keysPath, recursive: true);
        }
    }

    [Fact]
    public async Task Session_WithKeysPath_IsAcceptedAfterRestart()
    {
        var settings = new Dictionary<string, string?> { ["DataProtection:KeysPath"] = _keysPath };
        await using var first = new ApiFactory(sql, settings);
        var client = await ApiClient.CreateAsync(first);
        await client.RegisterAsync("Maria", "maria@example.com", ApiClient.DemoPassword);
        var login = await client.LoginAsync("maria@example.com", ApiClient.DemoPassword);
        var authCookie = AuthCookieFrom(login);

        await using var restarted = new ApiFactory(first.ConnectionString, settings);
        var http = restarted.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", authCookie);

        Assert.Equal(HttpStatusCode.OK, (await http.SendAsync(request, Ct)).StatusCode);
        Assert.NotEmpty(Directory.GetFiles(_keysPath, "key-*.xml"));
    }

    private static string AuthCookieFrom(HttpResponseMessage login) =>
        login.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{AuthCookieName}=", StringComparison.Ordinal))
            .Split(';')[0];
}
