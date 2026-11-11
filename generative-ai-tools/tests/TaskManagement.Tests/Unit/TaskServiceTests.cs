using TaskManagement.Api.Tasks;
using TaskManagement.Tests.Unit.Fakes;

namespace TaskManagement.Tests.Unit;

public sealed class TaskServiceTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Start = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTaskRepository _repository = new();
    private readonly ManualClock _clock = new(Start);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateAsync_ValidRequest_StoresTaskOwnedByCurrentUserWithTodoStatus()
    {
        var result = await ServiceFor(UserA).CreateAsync(
            new CreateTaskRequest("Plan lesson", "Unit 3", new DateOnly(2026, 12, 1)), Cancellation);

        var created = Assert.IsType<TaskResult<TaskResponse>.Success>(result).Value;
        var stored = Assert.Single(_repository.Tasks);
        Assert.Equal(UserA, stored.UserId);
        Assert.Equal(stored.Id, created.Id);
        Assert.Equal(TaskItemStatus.Todo, created.Status);
        Assert.Equal("Unit 3", created.Description);
        Assert.Equal(new DateOnly(2026, 12, 1), created.DueDate);
    }

    [Fact]
    public async Task CreateAsync_TitleWithSurroundingSpaces_StoresTrimmedTitle()
    {
        var result = await ServiceFor(UserA).CreateAsync(new CreateTaskRequest("  Plan lesson  ", null, null), Cancellation);

        Assert.Equal("Plan lesson", Assert.IsType<TaskResult<TaskResponse>.Success>(result).Value.Title);
        Assert.Equal("Plan lesson", Assert.Single(_repository.Tasks).Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankDescription_StoresNull(string description)
    {
        await ServiceFor(UserA).CreateAsync(new CreateTaskRequest("Plan lesson", description, null), Cancellation);

        Assert.Null(Assert.Single(_repository.Tasks).Description);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsCreatedAtAndUpdatedAtFromClock()
    {
        var result = await ServiceFor(UserA).CreateAsync(new CreateTaskRequest("Plan lesson", null, null), Cancellation);

        var created = Assert.IsType<TaskResult<TaskResponse>.Success>(result).Value;
        Assert.Equal(Start, created.CreatedAt);
        Assert.Equal(Start, created.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ReturnsFieldErrorsAndStoresNothing()
    {
        var result = await ServiceFor(UserA).CreateAsync(new CreateTaskRequest(" ", null, null), Cancellation);

        var invalid = Assert.IsType<TaskResult<TaskResponse>.ValidationFailed>(result);
        Assert.Equal(["title"], invalid.Errors.Keys);
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public async Task GetAsync_OwnTask_ReturnsIt()
    {
        var task = SeedTaskOwnedBy(UserA);

        var result = await ServiceFor(UserA).GetAsync(task.Id, Cancellation);

        Assert.Equal(task.Id, Assert.IsType<TaskResult<TaskResponse>.Success>(result).Value.Id);
    }

    [Fact]
    public async Task GetAsync_OtherUsersTask_ReturnsNotFound()
    {
        var task = SeedTaskOwnedBy(UserA);

        var result = await ServiceFor(UserB).GetAsync(task.Id, Cancellation);

        Assert.IsType<TaskResult<TaskResponse>.NotFound>(result);
    }

    [Fact]
    public async Task GetAsync_MissingTask_ReturnsNotFound()
    {
        var result = await ServiceFor(UserA).GetAsync(Guid.NewGuid(), Cancellation);

        Assert.IsType<TaskResult<TaskResponse>.NotFound>(result);
    }

    [Fact]
    public async Task UpdateAsync_OwnTask_ReplacesFieldsAndAdvancesUpdatedAtOnly()
    {
        var task = SeedTaskOwnedBy(UserA);
        _clock.Advance(TimeSpan.FromHours(2));

        var result = await ServiceFor(UserA).UpdateAsync(
            task.Id,
            new UpdateTaskRequest("  New title ", "  ", TaskItemStatus.Done, new DateOnly(2027, 1, 15)),
            Cancellation);

        var updated = Assert.IsType<TaskResult<TaskResponse>.Success>(result).Value;
        Assert.Equal("New title", updated.Title);
        Assert.Null(updated.Description);
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.Equal(new DateOnly(2027, 1, 15), updated.DueDate);
        Assert.Equal(Start, updated.CreatedAt);
        Assert.Equal(Start.AddHours(2), updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_OwnTask_SavesReplacedTaskThroughRepositoryOnce()
    {
        var task = SeedTaskOwnedBy(UserA);
        _clock.Advance(TimeSpan.FromHours(2));

        await ServiceFor(UserA).UpdateAsync(
            task.Id, new UpdateTaskRequest("New title", null, TaskItemStatus.Done, null), Cancellation);

        Assert.Equal(new RecordedUpdate(UserA, task.Id, "New title", Start.AddHours(2)), Assert.Single(_repository.Updates));
    }

    [Fact]
    public async Task UpdateAsync_OtherUsersTask_ReturnsNotFoundAndLeavesTaskUnchanged()
    {
        var task = SeedTaskOwnedBy(UserA);
        _clock.Advance(TimeSpan.FromHours(2));

        var result = await ServiceFor(UserB).UpdateAsync(
            task.Id, new UpdateTaskRequest("Taken over", null, TaskItemStatus.Done, null), Cancellation);

        Assert.IsType<TaskResult<TaskResponse>.NotFound>(result);
        Assert.Equal("Original title", task.Title);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal(Start, task.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_MissingTask_ReturnsNotFound()
    {
        var result = await ServiceFor(UserA).UpdateAsync(
            Guid.NewGuid(), new UpdateTaskRequest("Title", null, TaskItemStatus.Done, null), Cancellation);

        Assert.IsType<TaskResult<TaskResponse>.NotFound>(result);
    }

    [Fact]
    public async Task UpdateAsync_InvalidRequest_ReturnsFieldErrorsAndLeavesTaskUnchanged()
    {
        var task = SeedTaskOwnedBy(UserA);

        var result = await ServiceFor(UserA).UpdateAsync(
            task.Id, new UpdateTaskRequest(new string('t', 201), null, null, null), Cancellation);

        var invalid = Assert.IsType<TaskResult<TaskResponse>.ValidationFailed>(result);
        Assert.Equal(["status", "title"], invalid.Errors.Keys.Order());
        Assert.Equal("Original title", task.Title);
    }

    [Fact]
    public async Task DeleteAsync_OwnTask_RemovesIt()
    {
        var task = SeedTaskOwnedBy(UserA);

        var result = await ServiceFor(UserA).DeleteAsync(task.Id, Cancellation);

        Assert.IsType<TaskResult<Guid>.Success>(result);
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersTask_ReturnsNotFoundAndKeepsTask()
    {
        var task = SeedTaskOwnedBy(UserA);

        var result = await ServiceFor(UserB).DeleteAsync(task.Id, Cancellation);

        Assert.IsType<TaskResult<Guid>.NotFound>(result);
        Assert.Same(task, Assert.Single(_repository.Tasks));
    }

    [Fact]
    public async Task ListAsync_TasksOfTwoUsers_ReturnsOnlyCurrentUsersTasksWithPagingInfo()
    {
        var ownTask = SeedTaskOwnedBy(UserA);
        SeedTaskOwnedBy(UserB);

        var result = await ServiceFor(UserA).ListAsync(new TaskListQuery(), Cancellation);

        var page = Assert.IsType<TaskResult<PagedResponse<TaskResponse>>.Success>(result).Value;
        Assert.Equal(ownTask.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task ListAsync_StatusAndPaging_PassesThemToRepositoryForCurrentUser()
    {
        await ServiceFor(UserA).ListAsync(
            new TaskListQuery { Status = TaskItemStatus.InProgress, Page = 3, PageSize = 10 }, Cancellation);

        Assert.Equal(new RecordedListRequest(UserA, TaskItemStatus.InProgress, 3, 10), _repository.LastListRequest);
    }

    [Fact]
    public async Task ListAsync_InvalidPaging_ReturnsFieldErrorsWithoutQuerying()
    {
        var result = await ServiceFor(UserA).ListAsync(new TaskListQuery { Page = 0, PageSize = 101 }, Cancellation);

        var invalid = Assert.IsType<TaskResult<PagedResponse<TaskResponse>>.ValidationFailed>(result);
        Assert.Equal(["page", "pageSize"], invalid.Errors.Keys.Order());
        Assert.Null(_repository.LastListRequest);
    }

    private TaskService ServiceFor(Guid userId) => new(_repository, new FixedCurrentUser(userId), _clock);

    private TaskItem SeedTaskOwnedBy(Guid ownerId)
    {
        var task = TaskItem.Create(ownerId, "Original title", "Original description", null, _clock.GetUtcNow());
        _repository.Seed(task);
        return task;
    }
}
