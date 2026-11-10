using LessonOrganizer.Infrastructure.Persistence;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LessonOrganizer.Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly Dictionary<string, string?> _settings;

    public ApiFactory(SqlServerFixture sql, IReadOnlyDictionary<string, string?>? settings = null, string environment = "Testing")
        : this(sql.ConnectionStringFor($"api_{Guid.NewGuid():N}"), settings, environment)
    {
    }

    public ApiFactory(string connectionString, IReadOnlyDictionary<string, string?>? settings = null, string environment = "Testing")
    {
        ConnectionString = connectionString;
        _environment = environment;
        _settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:LessonOrganizer"] = connectionString,
            ["Database:MigrateOnStartup"] = "true",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
        };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            _settings[key] = value;
        }
    }

    public string ConnectionString { get; }

    public LessonOrganizerDbContext NewDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<LessonOrganizerDbContext>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }
    }
}
