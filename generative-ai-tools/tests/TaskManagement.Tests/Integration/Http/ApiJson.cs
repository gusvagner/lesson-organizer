using System.Net;
using System.Text;
using System.Text.Json;

namespace TaskManagement.Tests.Integration.Http;

public static class ApiJson
{
    public const string ProblemContentType = "application/problem+json";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    public static async Task<JsonElement> CreateTaskAsync(HttpClient client, string json)
    {
        using var response = await client.PostAsync("/api/tasks", Body(json), Cancellation);
        return await ReadAsync(response, HttpStatusCode.Created);
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        var body = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.True(response.StatusCode == expectedStatus, $"Expected {(int)expectedStatus}, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    public static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        var problem = await ReadAsync(response, expectedStatus);
        Assert.Equal(ProblemContentType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        return problem;
    }

    public static async Task<string[]> ReadValidationErrorKeysAsync(HttpResponseMessage response)
    {
        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        return problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order().ToArray();
    }

    public static string[] PropertyNames(JsonElement element) =>
        element.EnumerateObject().Select(property => property.Name).Order().ToArray();

    public static Guid Id(JsonElement task) => task.GetProperty("id").GetGuid();

    public static async Task<HttpResponseMessage> PutJsonAsync(HttpClient client, Guid taskId, string json) =>
        await client.PutAsync($"/api/tasks/{taskId}", Body(json), Cancellation);

    public static async Task<JsonElement> GetJsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, Cancellation);
        return await ReadAsync(response, HttpStatusCode.OK);
    }
}
