namespace TaskManagement.Api.Tasks;

public sealed class TaskItem
{
    private TaskItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem Create(Guid ownerId, string title, string? description, DateOnly? dueDate, DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        var task = new TaskItem { Id = Guid.NewGuid(), UserId = ownerId, CreatedAt = utcNow };
        task.Replace(title, description, TaskItemStatus.Todo, dueDate, utcNow);
        return task;
    }

    public void Replace(string title, string? description, TaskItemStatus status, DateOnly? dueDate, DateTimeOffset now)
    {
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = now.ToUniversalTime();
    }
}
