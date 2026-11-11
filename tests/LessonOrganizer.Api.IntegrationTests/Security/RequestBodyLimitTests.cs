using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Api.IntegrationTests.Security;

public sealed class RequestBodyLimitTests(SqlServerFixture sql) : IAsyncLifetime
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
    public async Task PostStudent_BodyOver1MiB_Returns413WithoutSavingAnything()
    {
        var teacher = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var oversized = new { name = "Ana", englishLevel = "Beginner", notes = new string('x', 1_100_000) };
        using var body = new StringContent(JsonSerializer.Serialize(oversized), Encoding.UTF8, "application/json");
        var response = await teacher.Http.PostAsync("/api/students", body, Ct);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await using var context = _factory.NewDbContext();
        Assert.Equal(0, await context.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task PostLessonPlan_WithLongestValidFields_Returns201()
    {
        var teacher = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var studentId = await teacher.CreateStudentAsync("Ana");
        var plan = new
        {
            studentId,
            title = new string('ç', 150),
            description = new string('ã', 5000),
            lessonDate = "2026-11-20",
        };
        var response = await teacher.Http.PostAsJsonAsync("/api/lesson-plans", plan, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
