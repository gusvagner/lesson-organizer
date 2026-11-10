using System.Net;
using System.Net.Http.Json;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LessonOrganizer.Api.IntegrationTests.Auth;

public sealed class PlainHttpTests(SqlServerFixture sql) : IAsyncLifetime
{
    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _factory = new ApiFactory(sql);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task CsrfAndRegister_OverPlainHttp_Work()
    {
        var http = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), HandleCookies = true });
        var csrf = await http.GetAsync("/api/auth/csrf", Ct);
        Assert.Equal(HttpStatusCode.NoContent, csrf.StatusCode);
        var cookie = csrf.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        http.DefaultRequestHeaders.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(cookie["XSRF-TOKEN=".Length..].Split(';')[0]));
        var register = await http.PostAsJsonAsync("/api/auth/register", new { name = "Maria", email = "maria@example.com", password = ApiClient.DemoPassword }, Ct);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
    }
}
