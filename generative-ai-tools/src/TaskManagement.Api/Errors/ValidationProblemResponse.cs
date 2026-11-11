using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskManagement.Api.Errors;

/// <summary>
/// Builds the 400 ValidationProblemDetails with every error keyed by its camelCase field name
/// (<c>$.status</c> → <c>status</c>, <c>PageSize</c> → <c>pageSize</c>).
/// </summary>
public static class ValidationProblemResponse
{
    private const string JsonPathPrefix = "$.";

    public static IActionResult Create(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var fieldErrors = ToFieldErrors(context.ModelState, BodyParameterNames(context));
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, fieldErrors, StatusCodes.Status400BadRequest);
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    private static ModelStateDictionary ToFieldErrors(ModelStateDictionary modelState, IReadOnlySet<string> bodyParameters)
    {
        var invalid = modelState.Where(entry => entry.Value is { Errors.Count: > 0 }).ToList();
        var hasFieldErrors = invalid.Any(entry => !bodyParameters.Contains(entry.Key));
        var fieldErrors = new ModelStateDictionary();
        foreach (var (key, entry) in invalid)
        {
            if (hasFieldErrors && bodyParameters.Contains(key))
            {
                continue;
            }

            AddErrors(fieldErrors, ToFieldName(key, bodyParameters), entry!.Errors);
        }

        return fieldErrors;
    }

    private static void AddErrors(ModelStateDictionary fieldErrors, string fieldName, ModelErrorCollection errors)
    {
        foreach (var error in errors)
        {
            fieldErrors.AddModelError(fieldName, string.IsNullOrEmpty(error.ErrorMessage) ? "The value is not valid." : error.ErrorMessage);
        }
    }

    private static string ToFieldName(string key, IReadOnlySet<string> bodyParameters)
    {
        if (bodyParameters.Contains(key) || key == "$")
        {
            return string.Empty;
        }

        var path = key.StartsWith(JsonPathPrefix, StringComparison.Ordinal) ? key[JsonPathPrefix.Length..] : key;
        return string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

    private static HashSet<string> BodyParameterNames(ActionContext context) =>
        context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.Ordinal);
}
