namespace LessonOrganizer.Domain.Common;

public sealed class Result
{
    private Result(IReadOnlyList<Error> errors) => Errors = errors;

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public static Result Success() => new([]);

    public static Result Failure(IEnumerable<Error> errors) => new(errors.ToArray());

    public static Result Failure(Error error) => new([error]);
}
