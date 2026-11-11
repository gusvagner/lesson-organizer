using TaskManagement.Api.Auth;

namespace TaskManagement.Api.Tasks;

public sealed class TaskService(ITaskRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<TaskResult<TaskResponse>> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var errors = TaskValidator.Validate(request);
        if (errors.Count > 0)
        {
            return new TaskResult<TaskResponse>.ValidationFailed(errors);
        }

        var task = TaskItem.Create(
            currentUser.UserId, request.Title!, request.Description, request.DueDate, timeProvider.GetUtcNow());
        await repository.AddAsync(task, cancellationToken);
        return new TaskResult<TaskResponse>.Success(ToResponse(task));
    }

    public async Task<TaskResult<TaskResponse>> GetAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await repository.FindAsync(currentUser.UserId, taskId, cancellationToken);
        return task is null
            ? new TaskResult<TaskResponse>.NotFound()
            : new TaskResult<TaskResponse>.Success(ToResponse(task));
    }

    public async Task<TaskResult<PagedResponse<TaskResponse>>> ListAsync(TaskListQuery query, CancellationToken cancellationToken)
    {
        var errors = TaskValidator.Validate(query);
        if (errors.Count > 0)
        {
            return new TaskResult<PagedResponse<TaskResponse>>.ValidationFailed(errors);
        }

        var page = await repository.ListAsync(
            currentUser.UserId, query.Status, query.Page, query.PageSize, cancellationToken);
        var items = page.Items.Select(ToResponse).ToList();
        return new TaskResult<PagedResponse<TaskResponse>>.Success(
            new PagedResponse<TaskResponse>(items, query.Page, query.PageSize, page.TotalCount));
    }

    public async Task<TaskResult<TaskResponse>> UpdateAsync(
        Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var errors = TaskValidator.Validate(request);
        if (errors.Count > 0)
        {
            return new TaskResult<TaskResponse>.ValidationFailed(errors);
        }

        var task = await repository.FindAsync(currentUser.UserId, taskId, cancellationToken);
        if (task is null)
        {
            return new TaskResult<TaskResponse>.NotFound();
        }

        task.Replace(request.Title!, request.Description, request.Status!.Value, request.DueDate, timeProvider.GetUtcNow());
        var saved = await repository.UpdateAsync(currentUser.UserId, task, cancellationToken);
        return saved
            ? new TaskResult<TaskResponse>.Success(ToResponse(task))
            : new TaskResult<TaskResponse>.NotFound();
    }

    public async Task<TaskResult<Guid>> DeleteAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(currentUser.UserId, taskId, cancellationToken);
        return deleted ? new TaskResult<Guid>.Success(taskId) : new TaskResult<Guid>.NotFound();
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate, task.CreatedAt, task.UpdatedAt);
}
