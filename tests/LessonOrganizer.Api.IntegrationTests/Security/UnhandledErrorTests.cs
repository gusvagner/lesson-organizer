using System.Net;
using System.Net.Http.Json;
using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LessonOrganizer.Api.IntegrationTests.Security;

public sealed class UnhandledErrorTests(SqlServerFixture sql)
{
    private const string SecretMessage = "secret-internal-detail Server=db;Password=hunter2";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnhandledException_OutsideDevelopment_Returns500WithoutDetails()
    {
        await using var factory = new ApiFactory(sql);
        var response = await RegisterAgainstAsync(factory, new ThrowingUserAccounts(new InvalidOperationException(SecretMessage)));
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("secret-internal-detail", body, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", body, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        SecurityHeaderAssertions.AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task BodyTooLargeWhileReading_Returns413NotAServerError()
    {
        await using var factory = new ApiFactory(sql);
        var tooLarge = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
        var response = await RegisterAgainstAsync(factory, new ThrowingUserAccounts(tooLarge));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        SecurityHeaderAssertions.AssertSecurityHeaders(response);
    }

    private static async Task<HttpResponseMessage> RegisterAgainstAsync(ApiFactory factory, IUserAccounts accounts)
    {
        await using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped(_ => accounts)));
        var http = failing.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
        var csrf = await http.GetAsync("/api/auth/csrf", Ct);
        var token = csrf.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split(';')[0]["XSRF-TOKEN=".Length..];
        http.DefaultRequestHeaders.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(token));
        var response = await http.PostAsJsonAsync(
            "/api/auth/register",
            new { name = "Maria", email = "maria@example.com", password = ApiClient.DemoPassword },
            Ct);
        await response.Content.LoadIntoBufferAsync(Ct);
        return response;
    }

    private sealed class ThrowingUserAccounts(Exception error) : IUserAccounts
    {
        public Task<Result<UserResponse>> CreateAsync(string name, string email, string password, CancellationToken ct) =>
            throw error;

        public Task<UserResponse?> FindByIdAsync(Guid id, CancellationToken ct) => throw error;

        public Task<LoginOutcome> CheckPasswordAsync(string email, string password, CancellationToken ct) => throw error;
    }
}
