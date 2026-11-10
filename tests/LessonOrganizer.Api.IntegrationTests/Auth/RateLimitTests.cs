using System.Net;
using LessonOrganizer.Testing.SqlServer;

namespace LessonOrganizer.Api.IntegrationTests.Auth;

public sealed class RateLimitTests(SqlServerFixture sql) : IAsyncLifetime
{
    private ApiFactory _factory = null!;

    public ValueTask InitializeAsync()
    {
        _factory = new ApiFactory(sql, new Dictionary<string, string?> { ["RateLimiting:Auth:PermitLimit"] = "3" });
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Login_FourthAttemptInWindow_Returns429()
    {
        var client = await ApiClient.CreateAsync(_factory);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync("nobody@example.com", "wrong-pass")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.LoginAsync("nobody@example.com", "wrong-pass")).StatusCode);
    }
}
