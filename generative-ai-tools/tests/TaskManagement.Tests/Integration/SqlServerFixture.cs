using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Tests.Integration;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace TaskManagement.Tests.Integration;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public TestDatabase CreateEmptyDatabase()
    {
        var connection = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"TaskManagement_{Guid.NewGuid():N}",
        };
        return new TestDatabase(connection.ConnectionString);
    }

    public async Task<TestDatabase> CreateMigratedDatabaseAsync(CancellationToken cancellationToken)
    {
        var database = CreateEmptyDatabase();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync(cancellationToken);
        return database;
    }
}
