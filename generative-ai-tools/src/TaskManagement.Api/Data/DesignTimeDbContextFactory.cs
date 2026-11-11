using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagement.Api.Data;

/// <summary>
/// Builds the context for <c>dotnet ef</c>: the connection string comes from this project's user secrets or the
/// <c>ConnectionStrings__TaskManagement</c> environment variable; without one, a placeholder that is enough for
/// <c>migrations add</c>.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskManagementDbContext>
{
    public const string MigrationsOnlyConnectionString = "Server=design-time-only;Database=TaskManagement";

    public TaskManagementDbContext CreateDbContext(string[] args) =>
        Create(new ConfigurationBuilder()
            .AddUserSecrets(typeof(DesignTimeDbContextFactory).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build());

    public static TaskManagementDbContext Create(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);
        var connectionString = string.IsNullOrWhiteSpace(configured) ? MigrationsOnlyConnectionString : configured;
        return new TaskManagementDbContext(
            new DbContextOptionsBuilder<TaskManagementDbContext>().UseSqlServer(connectionString).Options);
    }
}
