namespace TaskManagement.Api.Tasks;

public interface ITaskRepository
{
    Task<TaskItem?> FindAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskPage> ListAsync(Guid ownerId, TaskItemStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task UpdateAsync(Guid ownerId, TaskItem task, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken);
}

public sealed record TaskPage(IReadOnlyList<TaskItem> Items, int TotalCount);
