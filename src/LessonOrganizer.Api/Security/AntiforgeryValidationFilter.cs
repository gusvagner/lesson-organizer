using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LessonOrganizer.Api.Security;

public sealed class AntiforgeryValidationFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public const string ProblemType = "urn:lesson-organizer:problem:invalid-xsrf-token";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!IsUnsafe(context.HttpContext.Request.Method))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Type = ProblemType,
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid XSRF token",
                Detail = "The XSRF token is missing or invalid. Call GET /api/auth/csrf and send the X-XSRF-TOKEN header.",
            });
        }
    }

    private static bool IsUnsafe(string method) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method));
}
