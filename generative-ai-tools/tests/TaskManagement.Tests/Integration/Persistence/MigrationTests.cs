using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Tests.Integration.Persistence;

public sealed class MigrationTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migrate_EmptyDatabase_AppliesAllMigrationsAndCreatesUsersAndTasksTables()
    {
        var database = sqlServer.CreateEmptyDatabase();
        await using var context = database.CreateContext();

        await context.Database.MigrateAsync(Cancellation);

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync(Cancellation));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(Cancellation));
        var tables = await context.Database
            .SqlQuery<string>($"SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync(Cancellation);
        Assert.Contains("Users", tables);
        Assert.Contains("Tasks", tables);
    }

    [Fact]
    public async Task Model_ComparedWithLatestMigration_HasNoPendingChanges()
    {
        await using var context = sqlServer.CreateEmptyDatabase().CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }
}
