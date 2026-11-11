using LessonOrganizer.Testing.SqlServer;

using static LessonOrganizer.Api.IntegrationTests.Security.SecurityHeaderAssertions;

namespace LessonOrganizer.Api.IntegrationTests.Security;

public sealed class SecurityHeadersTests(SqlServerFixture sql) : IAsyncLifetime
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"lo-webroot-{Guid.NewGuid():N}");
    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<!doctype html><title>shell</title>");
        File.WriteAllText(Path.Combine(_webRoot, "favicon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        _factory = new ApiFactory(sql, new Dictionary<string, string?> { ["webroot"] = _webRoot });
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }

    [Theory]
    [InlineData("/students/1")]
    [InlineData("/favicon.svg")]
    [InlineData("/api/students")]
    [InlineData("/api/auth/csrf")]
    public async Task AnonymousResponse_HasSecurityHeaders(string path)
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        AssertSecurityHeaders(await anonymous.Http.GetAsync(path, Ct));
    }

    [Fact]
    public async Task ProblemDetailsResponse_HasSecurityHeaders()
    {
        var loggedIn = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        AssertSecurityHeaders(await loggedIn.Http.GetAsync($"/api/students/{Guid.CreateVersion7()}", Ct));
    }
}
