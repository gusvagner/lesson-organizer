using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Api.IntegrationTests.LessonPlans;

public sealed class LessonPlansEndpointsTests(SqlServerFixture sql) : IAsyncLifetime
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
    public async Task List_NumericUndefinedStatusInQuery_Returns400()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        Assert.Equal(HttpStatusCode.BadRequest, (await a.Http.GetAsync("/api/lesson-plans?status=7", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.Http.GetAsync("/api/lesson-plans?status=Done", Ct)).StatusCode);
    }

    [Fact]
    public async Task Create_Returns201ToPrepareWithStudentName()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var student = await a.CreateStudentAsync("Ana");
        var response = await a.Http.PostAsJsonAsync("/api/lesson-plans", Plan(student, "Review", "2026-11-12"), Ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/lesson-plans/{body.GetProperty("id").GetGuid()}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(("ToPrepare", "Ana"), (body.GetProperty("status").GetString(), body.GetProperty("studentName").GetString()));
    }

    [Fact]
    public async Task Create_DateAsIsoString_RoundTrips()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateLessonPlanAsync(await a.CreateStudentAsync("Ana"), "Review", "2026-11-12");
        var body = await a.Http.GetStringAsync($"/api/lesson-plans/{id}", Ct);
        Assert.Contains("\"lessonDate\":\"2026-11-12\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_OtherTeachersStudent_Returns400SameAsMissing()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var b = await ApiClient.LoggedInAsync(_factory, "b@example.com");
        var bStudent = await b.CreateStudentAsync("Bob");
        var foreign = await a.Http.PostAsJsonAsync("/api/lesson-plans", Plan(bStudent, "Review", "2026-11-12"), Ct);
        var missing = await a.Http.PostAsJsonAsync("/api/lesson-plans", Plan(Guid.CreateVersion7(), "Review", "2026-11-12"), Ct);
        Assert.Equal((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest), (foreign.StatusCode, missing.StatusCode));
        Assert.Equal(await StudentIdErrorsAsync(foreign), await StudentIdErrorsAsync(missing));
        Assert.Equal("[\"Student not found.\"]", await StudentIdErrorsAsync(foreign));
    }

    [Fact]
    public async Task ChangeStatus_IntegerStatusInBody_Returns400()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateLessonPlanAsync(await a.CreateStudentAsync("Ana"), "Review", "2026-11-12");
        var response = await a.Http.PatchAsJsonAsync($"/api/lesson-plans/{id}/status", new { status = 1 }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ToPrepare", await StatusOfAsync(a, id));
    }

    [Fact]
    public async Task ChangeStatus_AnyToAny_Returns200()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateLessonPlanAsync(await a.CreateStudentAsync("Ana"), "Review", "2026-11-12");
        await a.Http.PatchAsJsonAsync($"/api/lesson-plans/{id}/status", new { status = "Delivered" }, Ct);
        var back = await a.Http.PatchAsJsonAsync($"/api/lesson-plans/{id}/status", new { status = "ToPrepare" }, Ct);
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal("ToPrepare", await StatusOfAsync(a, id));
    }

    [Fact]
    public async Task Update_DoesNotChangeStatus()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var student = await a.CreateStudentAsync("Ana");
        var id = await a.CreateLessonPlanAsync(student, "Review", "2026-11-12");
        await a.Http.PatchAsJsonAsync($"/api/lesson-plans/{id}/status", new { status = "Ready" }, Ct);
        var response = await a.Http.PutAsJsonAsync($"/api/lesson-plans/{id}", Plan(student, "New title", "2026-11-13"), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Ready", await StatusOfAsync(a, id));
    }

    [Fact]
    public async Task TeacherB_GetsNotFoundOnEveryOperationOnTeacherAsPlan()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var b = await ApiClient.LoggedInAsync(_factory, "b@example.com");
        var aStudent = await a.CreateStudentAsync("Ana");
        var bStudent = await b.CreateStudentAsync("Bob");
        var id = await a.CreateLessonPlanAsync(aStudent, "Review", "2026-11-12");
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.GetAsync($"/api/lesson-plans/{id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.PutAsJsonAsync($"/api/lesson-plans/{id}", Plan(bStudent, "Hacked", "2026-11-12"), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.PatchAsJsonAsync($"/api/lesson-plans/{id}/status", new { status = "Delivered" }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.DeleteAsync($"/api/lesson-plans/{id}", Ct)).StatusCode);
        Assert.Equal("ToPrepare", await StatusOfAsync(a, id));
    }

    [Fact]
    public async Task List_FilterByOtherTeachersStudent_ReturnsEmpty()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var b = await ApiClient.LoggedInAsync(_factory, "b@example.com");
        var bStudent = await b.CreateStudentAsync("Bob");
        await b.CreateLessonPlanAsync(bStudent, "Review", "2026-11-12");
        var list = await a.Http.GetFromJsonAsync<List<JsonElement>>($"/api/lesson-plans?studentId={bStudent}", Ct);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task List_SortedByDateAndFiltersCombine()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var ana = await a.CreateStudentAsync("Ana");
        var bruno = await a.CreateStudentAsync("Bruno");
        await a.CreateLessonPlanAsync(ana, "Later", "2026-11-20");
        var ready = await a.CreateLessonPlanAsync(ana, "Earlier", "2026-11-10");
        await a.CreateLessonPlanAsync(bruno, "Bruno", "2026-11-11");
        await a.Http.PatchAsJsonAsync($"/api/lesson-plans/{ready}/status", new { status = "Ready" }, Ct);
        var all = await a.Http.GetFromJsonAsync<List<JsonElement>>("/api/lesson-plans", Ct);
        var filtered = await a.Http.GetFromJsonAsync<List<JsonElement>>($"/api/lesson-plans?studentId={ana}&status=Ready", Ct);
        Assert.Equal(["Earlier", "Bruno", "Later"], all!.Select(plan => plan.GetProperty("title").GetString()));
        Assert.Equal(["Earlier"], filtered!.Select(plan => plan.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task DeleteStudent_WithPlanCreatedOverHttp_Returns409()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var student = await a.CreateStudentAsync("Ana");
        await a.CreateLessonPlanAsync(student, "Review", "2026-11-12");
        Assert.Equal(HttpStatusCode.Conflict, (await a.Http.DeleteAsync($"/api/students/{student}", Ct)).StatusCode);
        await using var context = _factory.NewDbContext();
        Assert.Equal(1, await context.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task Delete_Own_Returns204AndRemoves()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateLessonPlanAsync(await a.CreateStudentAsync("Ana"), "Review", "2026-11-12");
        Assert.Equal(HttpStatusCode.NoContent, (await a.Http.DeleteAsync($"/api/lesson-plans/{id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.Http.GetAsync($"/api/lesson-plans/{id}", Ct)).StatusCode);
    }

    private static object Plan(Guid studentId, string title, string lessonDate) => new { studentId, title, lessonDate };

    private static async Task<string> StudentIdErrorsAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("studentId").GetRawText();

    private static async Task<string?> StatusOfAsync(ApiClient client, Guid id) =>
        (await client.Http.GetFromJsonAsync<JsonElement>($"/api/lesson-plans/{id}")).GetProperty("status").GetString();
}
