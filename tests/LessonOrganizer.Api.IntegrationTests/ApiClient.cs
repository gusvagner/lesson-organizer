using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LessonOrganizer.Api.IntegrationTests;

public sealed class ApiClient
{
    public const string DemoPassword = "Demo1234!";
    private const string XsrfHeader = "X-XSRF-TOKEN";
    private const string XsrfCookiePrefix = "XSRF-TOKEN=";

    private ApiClient(HttpClient http)
    {
        Http = http;
    }

    public HttpClient Http { get; }

    public static async Task<ApiClient> CreateAsync(ApiFactory factory)
    {
        var client = new ApiClient(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        }));
        await client.RefreshCsrfAsync();
        return client;
    }

    public static async Task<ApiClient> LoggedInAsync(ApiFactory factory, string email)
    {
        var client = await CreateAsync(factory);
        await client.RegisterAsync(email.Split('@')[0], email, DemoPassword);
        var login = await client.LoginAsync(email, DemoPassword);
        if (login.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Login of {email} returned {(int)login.StatusCode}. Expected 200.");
        }

        return client;
    }

    public async Task RefreshCsrfAsync()
    {
        var response = await Http.GetAsync("/api/auth/csrf");
        var cookie = XsrfCookie(response);
        var token = Uri.UnescapeDataString(cookie[XsrfCookiePrefix.Length..].Split(';')[0]);
        Http.DefaultRequestHeaders.Remove(XsrfHeader);
        Http.DefaultRequestHeaders.Add(XsrfHeader, token);
    }

    private static async Task<Guid> CreatedIdAsync(HttpResponseMessage response, string request)
    {
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"{request} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}. Expected 201.");
        }

        return (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
    }

    private static string XsrfCookie(HttpResponseMessage response)
    {
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];
        return cookies.SingleOrDefault(value => value.StartsWith(XsrfCookiePrefix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"GET /api/auth/csrf returned {(int)response.StatusCode} with cookies [{string.Join(" | ", cookies)}]. Expected 204 with an XSRF-TOKEN cookie.");
    }

    public Task<HttpResponseMessage> RegisterAsync(string name, string email, string password) =>
        Http.PostAsJsonAsync("/api/auth/register", new { name, email, password });

    public async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        var response = await Http.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (response.IsSuccessStatusCode)
        {
            await RefreshCsrfAsync();
        }

        return response;
    }

    public async Task<Guid> CreateStudentAsync(string name)
    {
        var response = await Http.PostAsJsonAsync("/api/students", new { name, englishLevel = "Intermediate" });
        return await CreatedIdAsync(response, $"POST /api/students for {name}");
    }

    public async Task<HttpResponseMessage> LogoutAsync()
    {
        var response = await Http.PostAsync("/api/auth/logout", null);
        await RefreshCsrfAsync();
        return response;
    }
}
