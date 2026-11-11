namespace TaskManagement.Api.Tasks;

public sealed record CreateTaskRequest(string? Title, string? Description, DateOnly? DueDate);

public sealed record UpdateTaskRequest(string? Title, string? Description, TaskItemStatus? Status, DateOnly? DueDate);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TaskListQuery
{
    public const int DefaultPageSize = 20;

    public TaskItemStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
