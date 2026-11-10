using System.Net;
using System.Text.Json;
using LessonOrganizer.Testing.SqlServer;

namespace LessonOrganizer.Api.IntegrationTests.Docs;

public sealed class ApiDocsTests(SqlServerFixture sql)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OpenApiDocument_InDevelopment_ListsAllEndpoints()
    {
        await using var factory = new ApiFactory(sql, environment: "Development");
        var anonymous = await ApiClient.CreateAsync(factory);
        var response = await anonymous.Http.GetAsync("/openapi/v1.json", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement.GetProperty("paths");
        foreach (var path in new[] { "/api/auth/login", "/api/students/{id}", "/api/lesson-plans/{id}/status" })
        {
            Assert.True(paths.TryGetProperty(path, out _), $"OpenAPI document has no path {path}.");
        }
    }

    [Fact]
    public async Task Scalar_InDevelopment_Returns200WithoutSession()
    {
        await using var factory = new ApiFactory(sql, environment: "Development");
        var anonymous = await ApiClient.CreateAsync(factory);
        var response = await anonymous.Http.GetAsync("/scalar", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiAndScalar_OutsideDevelopment_Return404()
    {
        await using var factory = new ApiFactory(sql);
        var loggedIn = await ApiClient.LoggedInAsync(factory, "a@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await loggedIn.Http.GetAsync("/openapi/v1.json", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await loggedIn.Http.GetAsync("/scalar", Ct)).StatusCode);
    }
}
