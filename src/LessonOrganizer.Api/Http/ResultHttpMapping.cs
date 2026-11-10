using LessonOrganizer.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Http;

public static class ResultHttpMapping
{
    public static IActionResult ToOk<T>(Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToProblem(result.Errors);

    public static IActionResult ToCreated<T>(Result<T> result, string actionName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? new CreatedAtActionResult(actionName, null, routeValues(result.Value), result.Value)
            : ToProblem(result.Errors);

    public static IActionResult ToNoContent(Result result) =>
        result.IsSuccess ? new NoContentResult() : ToProblem(result.Errors);

    public static IActionResult ToProblem(IReadOnlyList<Error> errors)
    {
        var validationErrors = errors.Where(error => error.Kind == ErrorKind.Validation).ToList();
        return validationErrors.Count > 0 ? ValidationProblem(validationErrors) : Problem(errors[0]);
    }

    private static ObjectResult ValidationProblem(List<Error> errors)
    {
        var byField = errors
            .GroupBy(error => error.Field ?? "")
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());
        var problem = new ValidationProblemDetails(byField) { Status = StatusCodes.Status400BadRequest };
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }

    private static ObjectResult Problem(Error error)
    {
        var (status, title) = error.Kind switch
        {
            ErrorKind.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorKind.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => throw new InvalidOperationException(
                $"Error kind '{error.Kind}' has no HTTP mapping. Expected Unauthorized, NotFound or Conflict."),
        };
        var problem = new ProblemDetails { Status = status, Title = title, Detail = error.Message };
        return new ObjectResult(problem) { StatusCode = status };
    }
}
