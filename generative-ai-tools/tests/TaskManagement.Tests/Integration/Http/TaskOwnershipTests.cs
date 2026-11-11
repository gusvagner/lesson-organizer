using System.Net;
using System.Text.Json;

namespace TaskManagement.Tests.Integration.Http;

public sealed class TaskOwnershipTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetTask_OtherUsersTask_Returns404LikeMissingTask()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var taskOfA = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"A's task"}""");

        using var foreign = await api.UserB.GetAsync($"/api/tasks/{ApiJson.Id(taskOfA)}", Cancellation);
        using var missing = await api.UserB.GetAsync($"/api/tasks/{Guid.NewGuid()}", Cancellation);

        AssertSameNotFound(
            await ApiJson.ReadProblemAsync(foreign, HttpStatusCode.NotFound),
            await ApiJson.ReadProblemAsync(missing, HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task PutTask_OtherUsersTask_Returns404LikeMissingTaskAndLeavesItUnchanged()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var taskOfA = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"A's task"}""");
        const string replacement = """{"title":"Taken over","status":"Done"}""";

        using var foreign = await ApiJson.PutJsonAsync(api.UserB, ApiJson.Id(taskOfA), replacement);
        using var missing = await ApiJson.PutJsonAsync(api.UserB, Guid.NewGuid(), replacement);

        AssertSameNotFound(
            await ApiJson.ReadProblemAsync(foreign, HttpStatusCode.NotFound),
            await ApiJson.ReadProblemAsync(missing, HttpStatusCode.NotFound));
        var stored = await ApiJson.GetJsonAsync(api.UserA, $"/api/tasks/{ApiJson.Id(taskOfA)}");
        Assert.Equal("A's task", stored.GetProperty("title").GetString());
        Assert.Equal("Todo", stored.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DeleteTask_OtherUsersTask_Returns404LikeMissingTaskAndKeepsIt()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var taskOfA = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"A's task"}""");

        using var foreign = await api.UserB.DeleteAsync($"/api/tasks/{ApiJson.Id(taskOfA)}", Cancellation);
        using var missing = await api.UserB.DeleteAsync($"/api/tasks/{Guid.NewGuid()}", Cancellation);

        AssertSameNotFound(
            await ApiJson.ReadProblemAsync(foreign, HttpStatusCode.NotFound),
            await ApiJson.ReadProblemAsync(missing, HttpStatusCode.NotFound));
        await ApiJson.GetJsonAsync(api.UserA, $"/api/tasks/{ApiJson.Id(taskOfA)}");
    }

    [Theory]
    [InlineData("/api/tasks")]
    [InlineData("/api/tasks?status=Todo")]
    public async Task ListTasks_OtherUsersTasks_AreNeverIncluded(string path)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        await ApiJson.CreateTaskAsync(api.UserA, """{"title":"A's first"}""");
        await ApiJson.CreateTaskAsync(api.UserA, """{"title":"A's second"}""");
        var taskOfB = await ApiJson.CreateTaskAsync(api.UserB, """{"title":"B's only"}""");

        var page = await ApiJson.GetJsonAsync(api.UserB, path);

        Assert.Equal([ApiJson.Id(taskOfB)], page.GetProperty("items").EnumerateArray().Select(ApiJson.Id));
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task PostTask_BodyNamingAnotherUser_IsOwnedByCaller()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        var created = await ApiJson.CreateTaskAsync(
            api.UserB, $$"""{"title":"Planted","userId":"{{api.UserAId}}","ownerId":"{{api.UserAId}}"}""");

        using var asOwnerA = await api.UserA.GetAsync($"/api/tasks/{ApiJson.Id(created)}", Cancellation);
        Assert.Equal(HttpStatusCode.NotFound, asOwnerA.StatusCode);
        await ApiJson.GetJsonAsync(api.UserB, $"/api/tasks/{ApiJson.Id(created)}");
    }

    private static void AssertSameNotFound(JsonElement foreign, JsonElement missing)
    {
        Assert.Equal("Task not found", foreign.GetProperty("title").GetString());
        Assert.Equal(missing.GetProperty("title").GetString(), foreign.GetProperty("title").GetString());
        Assert.Equal(missing.GetProperty("type").GetString(), foreign.GetProperty("type").GetString());
        Assert.Equal(ApiJson.PropertyNames(missing), ApiJson.PropertyNames(foreign));
    }
}
