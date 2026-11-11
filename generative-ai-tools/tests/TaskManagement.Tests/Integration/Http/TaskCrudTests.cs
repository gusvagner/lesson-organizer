using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Api.Data;
using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Integration.Http;

public sealed class TaskCrudTests(SqlServerFixture sqlServer)
{
    private static readonly string[] TaskPropertyNames =
        ["createdAt", "description", "dueDate", "id", "status", "title", "updatedAt"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PostTask_ValidBody_Returns201WithLocationAndNormalizedTodoTask()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.PostAsync(
            "/api/tasks",
            ApiJson.Body("""{"title":"  Plan lesson  ","description":"   ","dueDate":"2026-12-01","status":"Done"}"""),
            Cancellation);

        var created = await ApiJson.ReadAsync(response, HttpStatusCode.Created);
        Assert.Equal($"/api/tasks/{ApiJson.Id(created)}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(TaskPropertyNames, ApiJson.PropertyNames(created));
        Assert.Equal("Plan lesson", created.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("description").ValueKind);
        Assert.Equal("Todo", created.GetProperty("status").GetString());
        Assert.Equal("2026-12-01", created.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task GetTask_LocationOfCreatedTask_Returns200WithSameTask()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        using var createResponse = await api.UserA.PostAsync("/api/tasks", ApiJson.Body("""{"title":"Plan lesson"}"""), Cancellation);
        var created = await ApiJson.ReadAsync(createResponse, HttpStatusCode.Created);

        var fetched = await ApiJson.GetJsonAsync(api.UserA, createResponse.Headers.Location!.AbsolutePath);

        Assert.Equal(created.GetRawText(), fetched.GetRawText());
    }

    [Fact]
    public async Task Tasks_FullCrudRoundTrip_BehavesAsResource()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var created = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Plan lesson","description":"Unit 3"}""");
        var id = ApiJson.Id(created);

        var listed = await ApiJson.GetJsonAsync(api.UserA, "/api/tasks");
        Assert.Equal([id], listed.GetProperty("items").EnumerateArray().Select(ApiJson.Id));

        using var put = await ApiJson.PutJsonAsync(
            api.UserA, id, """{"title":"Plan lesson 3","description":null,"status":"InProgress","dueDate":"2026-12-05"}""");
        var updated = await ApiJson.ReadAsync(put, HttpStatusCode.OK);
        Assert.Equal("Plan lesson 3", updated.GetProperty("title").GetString());
        Assert.Equal("InProgress", updated.GetProperty("status").GetString());
        Assert.Equal(updated.GetRawText(), (await ApiJson.GetJsonAsync(api.UserA, $"/api/tasks/{id}")).GetRawText());

        using var delete = await api.UserA.DeleteAsync($"/api/tasks/{id}", Cancellation);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var afterDelete = await api.UserA.GetAsync($"/api/tasks/{id}", Cancellation);
        await ApiJson.ReadProblemAsync(afterDelete, HttpStatusCode.NotFound);
        using var secondDelete = await api.UserA.DeleteAsync($"/api/tasks/{id}", Cancellation);
        await ApiJson.ReadProblemAsync(secondDelete, HttpStatusCode.NotFound);
        Assert.Equal(0, (await ApiJson.GetJsonAsync(api.UserA, "/api/tasks")).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task PutTask_SameBodyTwice_LeavesTheSameRepresentation()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var id = ApiJson.Id(await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Plan lesson"}"""));
        const string body = """{"title":"Final","description":"Same","status":"Done","dueDate":"2026-12-24"}""";

        using var first = await ApiJson.PutJsonAsync(api.UserA, id, body);
        using var second = await ApiJson.PutJsonAsync(api.UserA, id, body);

        var afterFirst = await ApiJson.ReadAsync(first, HttpStatusCode.OK);
        var afterSecond = await ApiJson.ReadAsync(second, HttpStatusCode.OK);
        foreach (var field in new[] { "id", "title", "description", "status", "dueDate", "createdAt" })
        {
            Assert.Equal(afterFirst.GetProperty(field).GetRawText(), afterSecond.GetProperty(field).GetRawText());
        }
    }

    [Fact]
    public async Task ListTasks_PageOfCallersTasks_ReturnsPagedShapeInDueDateOrder()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var noDate = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"No date"}""");
        var late = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Late","dueDate":"2026-12-20"}""");
        var early = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Early","dueDate":"2026-12-02"}""");

        var firstPage = await ApiJson.GetJsonAsync(api.UserA, "/api/tasks?page=1&pageSize=2");
        var secondPage = await ApiJson.GetJsonAsync(api.UserA, "/api/tasks?page=2&pageSize=2");

        Assert.Equal(["items", "page", "pageSize", "totalCount"], ApiJson.PropertyNames(firstPage));
        Assert.Equal([ApiJson.Id(early), ApiJson.Id(late)], firstPage.GetProperty("items").EnumerateArray().Select(ApiJson.Id));
        Assert.Equal([ApiJson.Id(noDate)], secondPage.GetProperty("items").EnumerateArray().Select(ApiJson.Id));
        Assert.Equal((1, 2, 3), (firstPage.GetProperty("page").GetInt32(), firstPage.GetProperty("pageSize").GetInt32(), firstPage.GetProperty("totalCount").GetInt32()));
    }

    [Fact]
    public async Task ListTasks_WithoutPaging_UsesPageOneAndSize20()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        var page = await ApiJson.GetJsonAsync(api.UserA, "/api/tasks");

        Assert.Equal((1, 20), (page.GetProperty("page").GetInt32(), page.GetProperty("pageSize").GetInt32()));
    }

    [Theory]
    [InlineData("Done")]
    [InlineData("done")]
    public async Task ListTasks_StatusFilter_ReturnsOnlyMatchingTasks(string status)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var finished = ApiJson.Id(await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Finished"}"""));
        await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Open"}""");
        using var put = await ApiJson.PutJsonAsync(api.UserA, finished, """{"title":"Finished","status":"Done"}""");
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var page = await ApiJson.GetJsonAsync(api.UserA, $"/api/tasks?status={status}");

        Assert.Equal([finished], page.GetProperty("items").EnumerateArray().Select(ApiJson.Id));
    }

    [Fact]
    public async Task PutTask_TaskDeletedBetweenFindAndSave_Returns404()
    {
        await using var api = await TwoUserApi.StartAsync(
            sqlServer,
            Cancellation,
            services => services.AddScoped<ITaskRepository, RowDeletedBeforeUpdateTaskRepository>());
        var id = ApiJson.Id(await ApiJson.CreateTaskAsync(api.UserA, """{"title":"About to vanish"}"""));

        using var response = await ApiJson.PutJsonAsync(api.UserA, id, """{"title":"Too late","status":"Done"}""");

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("Task not found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UnknownRoute_WithValidToken_Returns404Problem()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.GetAsync("/api/tasks/not-a-guid", Cancellation);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    private sealed class RowDeletedBeforeUpdateTaskRepository(TaskManagementDbContext dbContext) : ITaskRepository
    {
        private readonly EfTaskRepository _inner = new(dbContext);

        public Task<TaskItem?> FindAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
            _inner.FindAsync(ownerId, taskId, cancellationToken);

        public Task<TaskPage> ListAsync(Guid ownerId, TaskItemStatus? status, int page, int pageSize, CancellationToken cancellationToken) =>
            _inner.ListAsync(ownerId, status, page, pageSize, cancellationToken);

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => _inner.AddAsync(task, cancellationToken);

        public async Task<bool> UpdateAsync(Guid ownerId, TaskItem task, CancellationToken cancellationToken)
        {
            await dbContext.Tasks.Where(stored => stored.Id == task.Id).ExecuteDeleteAsync(cancellationToken);
            return await _inner.UpdateAsync(ownerId, task, cancellationToken);
        }

        public Task<bool> DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
            _inner.DeleteAsync(ownerId, taskId, cancellationToken);
    }
}
