using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Tasks;
using TaskManagement.Api.Users;

namespace TaskManagement.Tests.Integration.Persistence;

public sealed class DatabaseConstraintTests(SqlServerFixture sqlServer)
{
    private const int ForeignKeyViolation = 547;
    private const int ConstraintViolation = 547;
    private const int NullNotAllowed = 515;
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int StringTruncated = 2628;

    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveTask_TitleOf200Characters_IsStored()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);

        await database.SeedTasksAsync(Cancellation, TaskItem.Create(user.Id, new string('t', 200), null, null, Now));

        await using var context = database.CreateContext();
        Assert.Equal(200, (await context.Tasks.SingleAsync(Cancellation)).Title.Length);
    }

    [Fact]
    public async Task SaveTask_TitleOf201Characters_IsRejectedByDatabase()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);

        var error = await SaveTaskExpectingSqlErrorAsync(database, TaskItem.Create(user.Id, new string('t', 201), null, null, Now));

        Assert.Equal(StringTruncated, error.Number);
    }

    [Fact]
    public async Task SaveTask_DescriptionOf2001Characters_IsRejectedByDatabase()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);

        var error = await SaveTaskExpectingSqlErrorAsync(
            database, TaskItem.Create(user.Id, "Title", new string('d', 2001), null, Now));

        Assert.Equal(StringTruncated, error.Number);
    }

    [Fact]
    public async Task SaveTask_OwnerNotInUsers_IsRejectedByForeignKey()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);

        var error = await SaveTaskExpectingSqlErrorAsync(database, TaskItem.Create(Guid.NewGuid(), "Title", null, null, Now));

        Assert.Equal(ForeignKeyViolation, error.Number);
    }

    [Fact]
    public async Task InsertTask_NullTitle_IsRejectedByDatabase()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);
        await using var context = database.CreateContext();

        var error = await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Tasks (Id, UserId, Title, Status, CreatedAt, UpdatedAt) VALUES ({Guid.NewGuid()}, {user.Id}, NULL, 'Todo', {Now}, {Now})",
            Cancellation));

        Assert.Equal(NullNotAllowed, error.Number);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InsertTask_BlankTitle_IsRejectedByCheckConstraint(string title)
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);
        await using var context = database.CreateContext();

        var error = await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Tasks (Id, UserId, Title, Status, CreatedAt, UpdatedAt) VALUES ({Guid.NewGuid()}, {user.Id}, {title}, 'Todo', {Now}, {Now})",
            Cancellation));

        Assert.Equal(ConstraintViolation, error.Number);
        Assert.Contains("CK_Tasks_Title_NotBlank", error.Message);
    }

    [Fact]
    public async Task SaveUser_DuplicateEmail_IsRejectedByUniqueIndex()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var existing = await database.SeedUserAsync(Cancellation);
        await using var context = database.CreateContext();
        context.Users.Add(new User(Guid.NewGuid(), existing.Email, "Someone else"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Cancellation));

        Assert.Equal(DuplicateKeyInUniqueIndex, Assert.IsType<SqlException>(exception.InnerException).Number);
    }

    [Fact]
    public async Task SaveTask_Status_IsStoredAsItsName()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);
        var task = TaskItem.Create(user.Id, "Title", null, null, Now);
        task.Replace("Title", null, TaskItemStatus.InProgress, null, Now);
        await database.SeedTasksAsync(Cancellation, task);

        await using var context = database.CreateContext();
        var stored = await context.Database.SqlQuery<string>($"SELECT Status AS [Value] FROM Tasks").SingleAsync(Cancellation);

        Assert.Equal("InProgress", stored);
    }

    [Fact]
    public async Task SaveTask_AllFields_RoundTripUnchanged()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var user = await database.SeedUserAsync(Cancellation);
        var task = TaskItem.Create(user.Id, "Title", "Description", new DateOnly(2026, 12, 24), Now.AddTicks(1234567));
        await database.SeedTasksAsync(Cancellation, task);

        await using var context = database.CreateContext();
        var stored = await context.Tasks.SingleAsync(Cancellation);

        Assert.Equal(
            (task.Id, task.UserId, task.Title, task.Description, task.Status, task.DueDate, task.CreatedAt, task.UpdatedAt),
            (stored.Id, stored.UserId, stored.Title, stored.Description, stored.Status, stored.DueDate, stored.CreatedAt, stored.UpdatedAt));
    }

    private static async Task<SqlException> SaveTaskExpectingSqlErrorAsync(TestDatabase database, TaskItem task)
    {
        await using var context = database.CreateContext();
        context.Tasks.Add(task);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Cancellation));
        return Assert.IsType<SqlException>(exception.InnerException);
    }
}
