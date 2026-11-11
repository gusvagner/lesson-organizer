using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Unit.Fakes;

public sealed class FakeTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = [];

    public IReadOnlyList<TaskItem> Tasks => _tasks;

    public RecordedListRequest? LastListRequest { get; private set; }

    public List<RecordedUpdate> Updates { get; } = [];

    private bool _deleteAfterNextFind;

    public void Seed(params TaskItem[] tasks) => _tasks.AddRange(tasks);

    public void DeleteAfterNextFind() => _deleteAfterNextFind = true;

    public Task<TaskItem?> FindAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken)
    {
        var found = _tasks.SingleOrDefault(task => task.Id == taskId && task.UserId == ownerId);
        if (_deleteAfterNextFind && found is not null)
        {
            _deleteAfterNextFind = false;
            _tasks.Remove(found);
        }

        return Task.FromResult(found);
    }

    public Task<TaskPage> ListAsync(
        Guid ownerId, TaskItemStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        LastListRequest = new RecordedListRequest(ownerId, status, page, pageSize);
        var matching = _tasks.Where(task => task.UserId == ownerId && (status is null || task.Status == status)).ToList();
        var items = matching.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new TaskPage(items, matching.Count));
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Guid ownerId, TaskItem task, CancellationToken cancellationToken)
    {
        if (task.UserId != ownerId)
        {
            throw new InvalidOperationException(
                $"Update of task {task.Id} owned by {task.UserId} was requested for owner {ownerId}; expected the same owner.");
        }

        Updates.Add(new RecordedUpdate(ownerId, task.Id, task.Title, task.UpdatedAt));
        return Task.FromResult(_tasks.Contains(task));
    }

    public Task<bool> DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
        Task.FromResult(_tasks.RemoveAll(task => task.Id == taskId && task.UserId == ownerId) > 0);
}

public sealed record RecordedListRequest(Guid OwnerId, TaskItemStatus? Status, int Page, int PageSize);

public sealed record RecordedUpdate(Guid OwnerId, Guid TaskId, string Title, DateTimeOffset UpdatedAt);
