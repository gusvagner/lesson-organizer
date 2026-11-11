using System.Data.Common;
using LessonOrganizer.Api.IntegrationTests.Logging;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LessonOrganizer.Api.IntegrationTests.Security;

public sealed class LogSecretsTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const string Email = "maria@example.com";
    private const string WrongPassword = "Wrong-Password-42";
    private const string EfCommandCategory = "Microsoft.EntityFrameworkCore.Database.Command";

    private readonly CapturingLoggerProvider _logs = new();
    private ApiFactory _factory = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _factory = new ApiFactory(sql, loggerProvider: _logs);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task FullFlow_LogsContainNoPasswordHashCookieTokenOrConnectionString()
    {
        var secrets = await RunFullFlowAsync();
        var texts = _logs.Logs.Select(log => log.Text).ToList();
        Assert.NotEmpty(texts);
        foreach (var (name, value) in secrets)
        {
            Assert.False(
                texts.Any(text => text.Contains(value, StringComparison.Ordinal)),
                $"A log message contains the {name}.");
        }
    }

    [Fact]
    public async Task EfCommands_NotLoggedAtInformation()
    {
        await RunFullFlowAsync();
        Assert.DoesNotContain(_logs.Logs, log => log.Category == EfCommandCategory && log.Level == LogLevel.Information);
    }

    private async Task<Dictionary<string, string>> RunFullFlowAsync()
    {
        var client = await ApiClient.CreateAsync(_factory);
        await client.RegisterAsync("Maria", Email, ApiClient.DemoPassword);
        await client.LoginAsync(Email, WrongPassword);
        var login = await client.LoginAsync(Email, ApiClient.DemoPassword);
        var studentId = await client.CreateStudentAsync("Ana");
        await client.CreateLessonPlanAsync(studentId, "Review", "2026-11-20");
        var xsrfToken = client.Http.DefaultRequestHeaders.GetValues("X-XSRF-TOKEN").Single();
        await client.LogoutAsync();
        return new Dictionary<string, string>
        {
            ["password"] = ApiClient.DemoPassword,
            ["wrong password"] = WrongPassword,
            ["password hash"] = await StoredHashAsync(),
            ["auth cookie"] = AuthCookieValue(login),
            ["XSRF token"] = xsrfToken,
            ["database password"] = DatabasePassword(),
        };
    }

    private async Task<string> StoredHashAsync()
    {
        await using var context = _factory.NewDbContext();
        return await context.Users.Select(user => user.PasswordHash!).SingleAsync(Ct);
    }

    private static string AuthCookieValue(HttpResponseMessage login) =>
        login.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal))
            .Split(';')[0]
            .Split('=', 2)[1];

    private string DatabasePassword() =>
        (string)new DbConnectionStringBuilder { ConnectionString = _factory.ConnectionString }["Password"];
}
