using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LessonOrganizer.Api.Http;

public static class InvalidInputResponse
{
    private const string JsonPathPrefix = "$.";

    public static IActionResult Create(ActionContext context)
    {
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var errors = FieldErrors(context.ModelState);
        if (errors.Keys.Any(key => !bodyParameters.Contains(key)))
        {
            errors = errors.Where(error => !bodyParameters.Contains(error.Key)).ToDictionary();
        }

        var problem = new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest };
        return new BadRequestObjectResult(problem);
    }

    public static string FieldName(string modelStateKey)
    {
        var key = modelStateKey.StartsWith(JsonPathPrefix, StringComparison.Ordinal)
            ? modelStateKey[JsonPathPrefix.Length..]
            : modelStateKey;
        return key.Length == 0 ? key : char.ToLowerInvariant(key[0]) + key[1..];
    }

    private static Dictionary<string, string[]> FieldErrors(ModelStateDictionary modelState) =>
        modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .GroupBy(entry => FieldName(entry.Key))
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(entry => entry.Value!.Errors).Select(ErrorMessage).ToArray());

    private static string ErrorMessage(ModelError error) =>
        string.IsNullOrEmpty(error.ErrorMessage) ? "The input was not valid." : error.ErrorMessage;
}
