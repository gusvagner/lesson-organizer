using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManagement.Tests.Integration;

public sealed class ApiFactory(
    IReadOnlyDictionary<string, string?> settings, Action<IServiceCollection>? replaceServices = null)
    : WebApplicationFactory<Program>
{
    public const string ConnectionStringKey = "ConnectionStrings:TaskManagement";
    public const string UnusedConnectionString = "Server=unused-in-this-test;Database=TaskManagement";

    public ApiFactory()
        : this(TestJwtSettings.Valid)
    {
    }

    public static ApiFactory For(TestDatabase database, Action<IServiceCollection>? replaceServices = null) =>
        new(TestJwtSettings.With(ConnectionStringKey, database.ConnectionString), replaceServices);

    public HttpClient CreateClientFor(Guid userId) => CreateClientWithToken(TestTokens.For(userId));

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(WithDefaults(settings)));
        if (replaceServices is not null)
        {
            builder.ConfigureTestServices(replaceServices);
        }
    }

    private static Dictionary<string, string?> WithDefaults(IReadOnlyDictionary<string, string?> overrides)
    {
        var merged = new Dictionary<string, string?>(overrides);
        merged.TryAdd(ConnectionStringKey, UnusedConnectionString);
        return merged;
    }
}
