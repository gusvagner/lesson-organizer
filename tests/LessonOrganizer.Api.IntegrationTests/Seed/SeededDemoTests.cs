using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LessonOrganizer.Testing.SqlServer;

namespace LessonOrganizer.Api.IntegrationTests.Seed;

public sealed class SeededDemoTests(SqlServerFixture sql) : IAsyncLifetime
{
    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _factory = new ApiFactory(sql, new Dictionary<string, string?> { ["Seed:DemoData"] = "true" });
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Maria_SeesOnlyHerFiveStudents()
    {
        var maria = await LoggedInAsync("maria@example.com");
        Assert.Equal(
            ["Ana Souza", "Bruno Lima", "Carla Mendes", "Diego Alves", "Fernanda Rocha"],
            await NamesAsync(maria, "/api/students", "name"));
    }

    [Fact]
    public async Task John_SeesNoneOfMariasData()
    {
        var john = await LoggedInAsync("john@example.com");
        Assert.Equal(["David", "Emma"], await NamesAsync(john, "/api/students", "name"));
        Assert.Equal(["David", "Emma", "David"], await NamesAsync(john, "/api/lesson-plans", "studentName"));
    }

    [Fact]
    public async Task DeleteAna_Returns409()
    {
        var maria = await LoggedInAsync("maria@example.com");
        var students = await maria.Http.GetFromJsonAsync<List<JsonElement>>("/api/students", Ct);
        var ana = students!.Single(student => student.GetProperty("name").GetString() == "Ana Souza").GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await maria.Http.DeleteAsync($"/api/students/{ana}", Ct)).StatusCode);
    }

    private async Task<ApiClient> LoggedInAsync(string email)
    {
        var client = await ApiClient.CreateAsync(_factory);
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(email, ApiClient.DemoPassword)).StatusCode);
        return client;
    }

    private static async Task<List<string?>> NamesAsync(ApiClient client, string path, string property) =>
        (await client.Http.GetFromJsonAsync<List<JsonElement>>(path))!.Select(item => item.GetProperty(property).GetString()).ToList();
}
