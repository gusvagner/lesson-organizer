using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TaskManagement.Tests.Integration;

public sealed class ApiFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<Program>
{
    public ApiFactory()
        : this(TestJwtSettings.Valid)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings));
    }
}
