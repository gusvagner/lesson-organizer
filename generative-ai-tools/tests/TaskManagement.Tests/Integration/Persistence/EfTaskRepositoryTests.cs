using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Integration.Persistence;

public sealed class EfTaskRepositoryTests(SqlServerFixture sqlServer)
{
    private static readonly DateTimeOffset Start = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FindAsync_OwnTask_ReturnsIt()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Mine", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);

        await using var context = database.CreateContext();
        var found = await new EfTaskRepository(context).FindAsync(owner, task.Id, Cancellation);

        Assert.Equal(task.Id, found?.Id);
    }

    [Fact]
    public async Task FindAsync_OtherUsersTask_ReturnsNull()
    {
        var (database, owner, otherUser) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Mine", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);

        await using var context = database.CreateContext();
        var found = await new EfTaskRepository(context).FindAsync(otherUser, task.Id, Cancellation);

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_TasksOfTwoUsers_ReturnsOnlyOwnersTasksAndCount()
    {
        var (database, owner, otherUser) = await CreateDatabaseWithTwoUsersAsync();
        var own = TaskItem.Create(owner, "Mine", null, null, Start);
        await database.SeedTasksAsync(Cancellation, own, TaskItem.Create(otherUser, "Theirs", null, null, Start));

        var page = await ListAsync(database, owner, null, 1, 20);

        Assert.Equal([own.Id], page.Items.Select(task => task.Id));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task ListAsync_OwnerWithoutTasks_ReturnsEmptyPageAndZeroCount()
    {
        var (database, owner, otherUser) = await CreateDatabaseWithTwoUsersAsync();
        await database.SeedTasksAsync(Cancellation, TaskItem.Create(otherUser, "Theirs", null, null, Start));

        var page = await ListAsync(database, owner, null, 1, 20);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task ListAsync_StatusFilter_ReturnsOnlyMatchingTasksAndTheirCount()
    {
        var (database, owner, otherUser) = await CreateDatabaseWithTwoUsersAsync();
        var done = WithStatus(TaskItem.Create(owner, "Done", null, null, Start), TaskItemStatus.Done);
        await database.SeedTasksAsync(
            Cancellation,
            done,
            TaskItem.Create(owner, "Todo", null, null, Start),
            WithStatus(TaskItem.Create(otherUser, "Theirs done", null, null, Start), TaskItemStatus.Done));

        var page = await ListAsync(database, owner, TaskItemStatus.Done, 1, 20);

        Assert.Equal([done.Id], page.Items.Select(task => task.Id));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task ListAsync_MixedDueDates_SortsByDueDateWithNoDateLastThenByCreationTime()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var lateFirstCreated = TaskItem.Create(owner, "late, created first", null, new DateOnly(2026, 12, 10), Start);
        var noDateFirstCreated = TaskItem.Create(owner, "no date, created second", null, null, Start.AddMinutes(1));
        var early = TaskItem.Create(owner, "early", null, new DateOnly(2026, 12, 1), Start.AddMinutes(2));
        var lateSecondCreated = TaskItem.Create(owner, "late, created later", null, new DateOnly(2026, 12, 10), Start.AddMinutes(3));
        var noDateLastCreated = TaskItem.Create(owner, "no date, created last", null, null, Start.AddMinutes(4));
        await database.SeedTasksAsync(
            Cancellation, noDateLastCreated, lateSecondCreated, early, noDateFirstCreated, lateFirstCreated);

        var page = await ListAsync(database, owner, null, 1, 20);

        Assert.Equal(
            [early.Id, lateFirstCreated.Id, lateSecondCreated.Id, noDateFirstCreated.Id, noDateLastCreated.Id],
            page.Items.Select(task => task.Id));
    }

    [Fact]
    public async Task ListAsync_SecondPage_ReturnsThatSliceInOrderWithTotalCount()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var tasks = Enumerable.Range(1, 5)
            .Select(day => TaskItem.Create(owner, $"Day {day}", null, new DateOnly(2026, 12, day), Start))
            .ToArray();
        await database.SeedTasksAsync(Cancellation, tasks);

        var page = await ListAsync(database, owner, null, 2, 2);

        Assert.Equal([tasks[2].Id, tasks[3].Id], page.Items.Select(task => task.Id));
        Assert.Equal(5, page.TotalCount);
    }

    [Fact]
    public async Task ListAsync_HighestPageTheValidatorAllows_ReturnsEmptyPageWithCount()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        await database.SeedTasksAsync(Cancellation, TaskItem.Create(owner, "Only", null, null, Start));

        var page = await ListAsync(database, owner, null, int.MaxValue, 1);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task AddAsync_NewTask_IsPersisted()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "New", "Details", new DateOnly(2026, 12, 1), Start);

        await using (var context = database.CreateContext())
        {
            await new EfTaskRepository(context).AddAsync(task, Cancellation);
        }

        await using var verification = database.CreateContext();
        var stored = await verification.Tasks.SingleAsync(Cancellation);
        Assert.Equal((task.Id, owner, "New", "Details"), (stored.Id, stored.UserId, stored.Title, stored.Description));
    }

    [Fact]
    public async Task UpdateAsync_FoundAndReplacedTask_PersistsChanges()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Before", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);

        await using (var context = database.CreateContext())
        {
            var repository = new EfTaskRepository(context);
            var found = await repository.FindAsync(owner, task.Id, Cancellation);
            found!.Replace("After", "Now with details", TaskItemStatus.Done, new DateOnly(2027, 1, 2), Start.AddHours(1));
            Assert.True(await repository.UpdateAsync(owner, found, Cancellation));
        }

        await using var verification = database.CreateContext();
        var stored = await verification.Tasks.SingleAsync(Cancellation);
        Assert.Equal(
            ("After", "Now with details", TaskItemStatus.Done, new DateOnly(2027, 1, 2), Start, Start.AddHours(1)),
            (stored.Title, stored.Description, stored.Status, stored.DueDate, stored.CreatedAt, stored.UpdatedAt));
    }

    [Fact]
    public async Task UpdateAsync_TaskDeletedAfterFind_ReturnsFalse()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Before", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);
        await using var context = database.CreateContext();
        var repository = new EfTaskRepository(context);
        var found = await repository.FindAsync(owner, task.Id, Cancellation);
        await using (var concurrentRequest = database.CreateContext())
        {
            await new EfTaskRepository(concurrentRequest).DeleteAsync(owner, task.Id, Cancellation);
        }

        found!.Replace("After", null, TaskItemStatus.Done, null, Start.AddHours(1));
        var updated = await repository.UpdateAsync(owner, found, Cancellation);

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateAsync_OwnerDifferentFromTaskOwner_ThrowsAndSavesNothing()
    {
        var (database, owner, otherUser) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Before", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);

        await using (var context = database.CreateContext())
        {
            var repository = new EfTaskRepository(context);
            var found = await repository.FindAsync(owner, task.Id, Cancellation);
            found!.Replace("After", null, TaskItemStatus.Done, null, Start.AddHours(1));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateAsync(otherUser, found, Cancellation));
            Assert.Contains(otherUser.ToString(), exception.Message);
        }

        await using var verification = database.CreateContext();
        Assert.Equal("Before", (await verification.Tasks.SingleAsync(Cancellation)).Title);
    }

    [Fact]
    public async Task DeleteAsync_OwnTask_RemovesItAndReturnsTrue()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Mine", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);

        bool deleted;
        await using (var context = database.CreateContext())
        {
            deleted = await new EfTaskRepository(context).DeleteAsync(owner, task.Id, Cancellation);
        }

        Assert.True(deleted);
        await using var verification = database.CreateContext();
        Assert.False(await verification.Tasks.AnyAsync(Cancellation));
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersTask_ReturnsFalseAndKeepsIt()
    {
        var (database, owner, otherUser) = await CreateDatabaseWithTwoUsersAsync();
        var task = TaskItem.Create(owner, "Mine", null, null, Start);
        await database.SeedTasksAsync(Cancellation, task);

        bool deleted;
        await using (var context = database.CreateContext())
        {
            deleted = await new EfTaskRepository(context).DeleteAsync(otherUser, task.Id, Cancellation);
        }

        Assert.False(deleted);
        await using var verification = database.CreateContext();
        Assert.True(await verification.Tasks.AnyAsync(stored => stored.Id == task.Id, Cancellation));
    }

    [Fact]
    public async Task DeleteAsync_MissingTask_ReturnsFalse()
    {
        var (database, owner, _) = await CreateDatabaseWithTwoUsersAsync();
        await using var context = database.CreateContext();

        Assert.False(await new EfTaskRepository(context).DeleteAsync(owner, Guid.NewGuid(), Cancellation));
    }

    private async Task<(TestDatabase Database, Guid Owner, Guid OtherUser)> CreateDatabaseWithTwoUsersAsync()
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(Cancellation);
        var owner = await database.SeedUserAsync(Cancellation);
        var otherUser = await database.SeedUserAsync(Cancellation);
        return (database, owner.Id, otherUser.Id);
    }

    private static async Task<TaskPage> ListAsync(
        TestDatabase database, Guid ownerId, TaskItemStatus? status, int page, int pageSize)
    {
        await using var context = database.CreateContext();
        return await new EfTaskRepository(context).ListAsync(ownerId, status, page, pageSize, Cancellation);
    }

    private static TaskItem WithStatus(TaskItem task, TaskItemStatus status)
    {
        task.Replace(task.Title, task.Description, status, task.DueDate, task.UpdatedAt);
        return task;
    }
}
