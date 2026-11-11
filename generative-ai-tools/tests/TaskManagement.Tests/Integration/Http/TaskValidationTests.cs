using System.Net;

namespace TaskManagement.Tests.Integration.Http;

public sealed class TaskValidationTests(SqlServerFixture sqlServer)
{
    private static readonly string LongTitle = new('t', 201);
    private static readonly string LongDescription = new('d', 2001);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public static TheoryData<string, string[]> InvalidCreateBodies => new()
    {
        { """{"description":"no title"}""", ["title"] },
        { """{"title":"   "}""", ["title"] },
        { $$"""{"title":"{{LongTitle}}"}""", ["title"] },
        { $$"""{"title":"Title","description":"{{LongDescription}}"}""", ["description"] },
        { """{"title":"Title","dueDate":"2026-13-45"}""", ["dueDate"] },
        { $$"""{"title":"{{LongTitle}}","description":"{{LongDescription}}"}""", ["description", "title"] },
    };

    public static TheoryData<string, string[]> InvalidUpdateBodies => new()
    {
        { """{"title":"Title","status":"Archived"}""", ["status"] },
        { """{"title":"Title","status":2}""", ["status"] },
        { """{"title":"Title","status":"2"}""", ["status"] },
        { """{"title":"Title","status":"Todo, InProgress"}""", ["status"] },
        { """{"title":"Title"}""", ["status"] },
        { """{"title":"","status":"Done"}""", ["title"] },
        { """{"title":"Title","status":"Done","dueDate":"tomorrow"}""", ["dueDate"] },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateBodies))]
    public async Task PostTask_InvalidBody_Returns400WithCamelCaseFieldErrors(string body, string[] expectedKeys)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.PostAsync("/api/tasks", ApiJson.Body(body), Cancellation);

        Assert.Equal(expectedKeys, await ApiJson.ReadValidationErrorKeysAsync(response));
    }

    [Theory]
    [MemberData(nameof(InvalidUpdateBodies))]
    public async Task PutTask_InvalidBody_Returns400WithCamelCaseFieldErrors(string body, string[] expectedKeys)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var task = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Original"}""");

        using var response = await ApiJson.PutJsonAsync(api.UserA, ApiJson.Id(task), body);

        Assert.Equal(expectedKeys, await ApiJson.ReadValidationErrorKeysAsync(response));
    }

    [Theory]
    [InlineData("?status=Archived", "status")]
    [InlineData("?status=2", "status")]
    [InlineData("?status=99", "status")]
    [InlineData("?status=Todo,InProgress", "status")]
    [InlineData("?page=0", "page")]
    [InlineData("?page=-3", "page")]
    [InlineData("?page=abc", "page")]
    [InlineData("?page=21474838&pageSize=100", "page")]
    [InlineData("?pageSize=0", "pageSize")]
    [InlineData("?pageSize=101", "pageSize")]
    [InlineData("?pageSize=ten", "pageSize")]
    public async Task ListTasks_InvalidQuery_Returns400WithCamelCaseFieldError(string query, string expectedKey)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.GetAsync($"/api/tasks{query}", Cancellation);

        Assert.Equal([expectedKey], await ApiJson.ReadValidationErrorKeysAsync(response));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    public async Task PostTask_BodyThatIsNotATaskObject_Returns400WithWholeBodyErrorKey(string body)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.PostAsync("/api/tasks", ApiJson.Body(body), Cancellation);

        Assert.Equal([""], await ApiJson.ReadValidationErrorKeysAsync(response));
    }

    [Fact]
    public async Task PostTask_JsonCutOffInsideAField_Returns400KeyedByThatField()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.PostAsync("/api/tasks", ApiJson.Body("""{"title":"""), Cancellation);

        Assert.Equal(["title"], await ApiJson.ReadValidationErrorKeysAsync(response));
    }

    [Fact]
    public async Task PutTask_NumericStatus_ErrorMessageDoesNotExposeSerializerDetails()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        var task = await ApiJson.CreateTaskAsync(api.UserA, """{"title":"Original"}""");

        using var response = await ApiJson.PutJsonAsync(api.UserA, ApiJson.Id(task), """{"title":"Title","status":2}""");

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.BadRequest);
        var message = Assert.Single(problem.GetProperty("errors").GetProperty("status").EnumerateArray()).GetString();
        Assert.DoesNotContain("System.", message);
        Assert.DoesNotContain("Path:", message);
        Assert.DoesNotContain("LineNumber", message);
    }
}
