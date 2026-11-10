using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Api.IntegrationTests.Students;

public sealed class StudentsEndpointsTests(SqlServerFixture sql) : IAsyncLifetime
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
    public async Task TeacherB_GetsNotFoundOnEveryOperationOnTeacherAsStudent()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var b = await ApiClient.LoggedInAsync(_factory, "b@example.com");
        var id = await a.CreateStudentAsync("Ana");
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.GetAsync($"/api/students/{id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.PutAsJsonAsync($"/api/students/{id}", Student("Hacked"), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Http.DeleteAsync($"/api/students/{id}", Ct)).StatusCode);
        Assert.Empty(await b.Http.GetFromJsonAsync<List<JsonElement>>("/api/students?search=Ana", Ct) ?? []);
        var stored = await a.Http.GetFromJsonAsync<JsonElement>($"/api/students/{id}", Ct);
        Assert.Equal("Ana", stored.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsCamelCaseJsonWithEnumName()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateStudentAsync("Ana");
        var body = await a.Http.GetStringAsync($"/api/students/{id}", Ct);
        Assert.Contains("\"englishLevel\":\"Intermediate\"", body, StringComparison.Ordinal);
        Assert.Contains("\"lessonPlanCount\":0", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_ReturnsLocationOfNewStudent()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var response = await a.Http.PostAsJsonAsync("/api/students", Student("Ana"), Ct);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
        Assert.Equal($"/api/students/{id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Create_IntegerEnglishLevel_Returns400AndStoresNothing()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var response = await a.Http.PostAsJsonAsync("/api/students", new { name = "Ana", englishLevel = 3 }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var context = _factory.NewDbContext();
        Assert.Equal(0, await context.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400ProblemWithoutStackTrace()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var response = await a.Http.PostAsync("/api/students", new StringContent("{ \"name\": ", Encoding.UTF8, "application/json"), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_NonGuidId_Returns404()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await a.Http.GetAsync("/api/students/abc", Ct)).StatusCode);
    }

    [Fact]
    public async Task Endpoints_WithoutSession_Return401()
    {
        var anonymous = await ApiClient.CreateAsync(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Http.GetAsync("/api/students", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Http.PostAsJsonAsync("/api/students", Student("Ana"), Ct)).StatusCode);
    }

    [Fact]
    public async Task Search_IgnoresCase()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        await a.CreateStudentAsync("Ana");
        await a.CreateStudentAsync("Mariana");
        await a.CreateStudentAsync("Bruno");
        var list = await a.Http.GetFromJsonAsync<List<JsonElement>>("/api/students?search=ANA", Ct);
        Assert.Equal(["Ana", "Mariana"], list!.Select(student => student.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Update_InvalidEmail_Returns400WithEmailError()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateStudentAsync("Ana");
        var response = await a.Http.PutAsJsonAsync($"/api/students/{id}", new { name = "Ana", email = "Ana <ana@example.com>", englishLevel = "Beginner" }, Ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Email is not a valid email address.", problem.GetProperty("errors").GetProperty("email")[0].GetString());
    }

    [Fact]
    public async Task Delete_WithLessonPlans_Returns409WithMessage()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        var id = await a.CreateStudentAsync("Ana");
        await using (var context = _factory.NewDbContext())
        {
            context.LessonPlans.Add(LessonPlan.Create(id, "Review", null, new DateOnly(2026, 11, 12)).Value);
            await context.SaveChangesAsync(Ct);
        }

        var response = await a.Http.DeleteAsync($"/api/students/{id}", Ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("This student has lesson plans. Delete them first or move them to another student.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Data_SurvivesNewFactoryOnSameDatabase()
    {
        var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
        await a.CreateStudentAsync("Ana");
        await using var restarted = new ApiFactory(_factory.ConnectionString);
        var again = await ApiClient.CreateAsync(restarted);
        await again.LoginAsync("a@example.com", ApiClient.DemoPassword);
        var list = await again.Http.GetFromJsonAsync<List<JsonElement>>("/api/students", Ct);
        Assert.Equal("Ana", Assert.Single(list!).GetProperty("name").GetString());
    }

    private static object Student(string name) => new { name, englishLevel = "Intermediate" };
}
