namespace TaskManagement.Api.Tasks;

public abstract record TaskResult<T>
{
    private TaskResult()
    {
    }

    public sealed record Success(T Value) : TaskResult<T>;

    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : TaskResult<T>;

    public sealed record NotFound : TaskResult<T>;
}
