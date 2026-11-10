using System.Security.Claims;
using LessonOrganizer.Application.Abstractions;

namespace LessonOrganizer.Api.Security;

public sealed class ClaimsCurrentTeacher(IHttpContextAccessor accessor) : ICurrentTeacher
{
    public Guid Id
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException(
                    $"The current user has no valid NameIdentifier claim (found '{value}'). Expected a Guid user id.");
        }
    }
}
