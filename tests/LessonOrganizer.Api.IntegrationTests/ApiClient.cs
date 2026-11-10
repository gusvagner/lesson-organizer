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
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(XsrfCookiePrefix, StringComparison.Ordinal));
        var token = Uri.UnescapeDataString(cookie[XsrfCookiePrefix.Length..].Split(';')[0]);
        Http.DefaultRequestHeaders.Remove(XsrfHeader);
        Http.DefaultRequestHeaders.Add(XsrfHeader, token);
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

    public async Task<HttpResponseMessage> LogoutAsync()
    {
        var response = await Http.PostAsync("/api/auth/logout", null);
        await RefreshCsrfAsync();
        return response;
    }
}
