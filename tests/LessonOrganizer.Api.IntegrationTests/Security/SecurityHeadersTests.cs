using LessonOrganizer.Testing.SqlServer;

namespace LessonOrganizer.Api.IntegrationTests.Security;

public sealed class SecurityHeadersTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; " +
        "frame-ancestors 'none'";

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

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", HeaderValue(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", HeaderValue(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", HeaderValue(response, "Referrer-Policy"));
        Assert.Equal(ContentSecurityPolicy, HeaderValue(response, "Content-Security-Policy"));
    }

    private static string? HeaderValue(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
