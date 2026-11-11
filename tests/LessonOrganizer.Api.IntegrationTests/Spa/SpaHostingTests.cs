using System.Net;
using LessonOrganizer.Testing.SqlServer;

namespace LessonOrganizer.Api.IntegrationTests.Spa;

public sealed class SpaHostingTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const string ShellMarker = "lesson-organizer-test-shell";

    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"lo-webroot-{Guid.NewGuid():N}");
    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), $"<!doctype html><title>{ShellMarker}</title>");
        File.WriteAllText(Path.Combine(_webRoot, "favicon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        _factory = new ApiFactory(sql, new Dictionary<string, string?> { ["webroot"] = _webRoot });
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }

    [Fact]
    public async Task SpaRoute_WithoutSession_ReturnsIndexHtml()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        var response = await anonymous.Http.GetAsync("/students/123", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(ShellMarker, await response.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_WithoutSession_ReturnsIndexHtml()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        var body = await anonymous.Http.GetStringAsync("/", Ct);
        Assert.Contains(ShellMarker, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaticFile_WithoutSession_ReturnsFile()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        var response = await anonymous.Http.GetAsync("/favicon.svg", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownApiPath_WithoutSession_Returns401()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Http.GetAsync("/api/nope", Ct)).StatusCode);
    }

    [Fact]
    public async Task UnknownApiPath_WithSession_Returns404NotHtml()
    {
        var loggedIn = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var response = await loggedIn.Http.GetAsync("/api/nope", Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(ShellMarker, await response.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task IndexHtml_IsRevalidatedByTheBrowser()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        var response = await anonymous.Http.GetAsync("/lesson-plans", Ct);
        Assert.True(response.Headers.CacheControl?.NoCache, "index.html must be sent with Cache-Control: no-cache.");
    }

    [Fact]
    public async Task ApiEndpoint_WithoutSession_Still401()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Http.GetAsync("/api/students", Ct)).StatusCode);
    }
}
