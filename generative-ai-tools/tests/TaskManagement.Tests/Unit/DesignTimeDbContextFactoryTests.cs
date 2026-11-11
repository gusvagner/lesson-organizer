using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskManagement.Api.Data;

namespace TaskManagement.Tests.Unit;

public sealed class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void Create_ConnectionStringConfigured_UsesIt()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DatabaseOptions.ConnectionStringKey] = "Server=from-user-secrets;Database=TaskManagement",
            })
            .Build();

        using var context = DesignTimeDbContextFactory.Create(configuration);

        Assert.Equal("from-user-secrets", ServerOf(context));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NoOrBlankConnectionString_UsesMigrationsOnlyPlaceholder(string? configured)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DatabaseOptions.ConnectionStringKey] = configured })
            .Build();

        using var context = DesignTimeDbContextFactory.Create(configuration);

        Assert.Equal(
            new SqlConnectionStringBuilder(DesignTimeDbContextFactory.MigrationsOnlyConnectionString).DataSource,
            ServerOf(context));
    }

    private static string ServerOf(DbContext context) =>
        new SqlConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;
}
