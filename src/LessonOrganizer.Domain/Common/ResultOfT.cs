namespace LessonOrganizer.Domain.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, IReadOnlyList<Error> errors)
    {
        _value = value;
        Errors = errors;
    }

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot read the value of a failed result. Errors: {string.Join(" ", Errors.Select(error => error.Message))}");

    public static Result<T> Success(T value) => new(value, []);

    public static Result<T> Failure(IEnumerable<Error> errors) => new(default, errors.ToArray());

    public static Result<T> Failure(Error error) => new(default, [error]);
}
