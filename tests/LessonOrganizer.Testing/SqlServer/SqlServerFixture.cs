using System.Data.Common;
using Testcontainers.MsSql;
using Xunit;

namespace LessonOrganizer.Testing.SqlServer;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public string ConnectionStringFor(string database)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = _container.GetConnectionString() };
        builder.Remove("Initial Catalog");
        builder["Database"] = database;
        return builder.ConnectionString;
    }

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
