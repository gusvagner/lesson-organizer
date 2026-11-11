namespace TaskManagement.Api.Tasks;

public static class TaskValidator
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int MaxPageSize = 100;

    private static readonly string AllowedStatuses = string.Join(", ", Enum.GetNames<TaskItemStatus>());

    public static IReadOnlyDictionary<string, string[]> Validate(CreateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddTitleError(errors, request.Title);
        AddDescriptionError(errors, request.Description);
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(UpdateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddTitleError(errors, request.Title);
        AddDescriptionError(errors, request.Description);
        AddMissingStatusError(errors, request.Status);
        AddUndefinedStatusError(errors, request.Status);
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(TaskListQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        AddUndefinedStatusError(errors, query.Status);
        AddPagingErrors(errors, query.Page, query.PageSize);
        return errors;
    }

    private static void AddTitleError(Dictionary<string, string[]> errors, string? title)
    {
        var trimmedLength = title?.Trim().Length ?? 0;
        if (trimmedLength == 0)
        {
            errors["title"] = ["title is required; got an empty or missing value."];
        }
        else if (trimmedLength > TitleMaxLength)
        {
            errors["title"] = [$"title must be at most {TitleMaxLength} characters after trimming; got {trimmedLength}."];
        }
    }

    private static void AddDescriptionError(Dictionary<string, string[]> errors, string? description)
    {
        if (!string.IsNullOrWhiteSpace(description) && description.Length > DescriptionMaxLength)
        {
            errors["description"] = [$"description must be at most {DescriptionMaxLength} characters; got {description.Length}."];
        }
    }

    private static void AddMissingStatusError(Dictionary<string, string[]> errors, TaskItemStatus? status)
    {
        if (status is null)
        {
            errors["status"] = [$"status is required; expected one of {AllowedStatuses}."];
        }
    }

    private static void AddUndefinedStatusError(Dictionary<string, string[]> errors, TaskItemStatus? status)
    {
        if (status is { } value && !Enum.IsDefined(value))
        {
            errors["status"] = [$"status must be one of {AllowedStatuses}; got {(int)value}."];
        }
    }

    private static void AddPagingErrors(Dictionary<string, string[]> errors, int page, int pageSize)
    {
        if (page < 1)
        {
            errors["page"] = [$"page must be 1 or greater; got {page}."];
        }
        else if (OffsetExceedsInt(page, pageSize))
        {
            errors["page"] = [$"page is too large for pageSize {pageSize}: (page - 1) * pageSize must not exceed {int.MaxValue}; got page {page}."];
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}; got {pageSize}."];
        }
    }

    private static bool OffsetExceedsInt(int page, int pageSize) => (long)(page - 1) * pageSize > int.MaxValue;
}
