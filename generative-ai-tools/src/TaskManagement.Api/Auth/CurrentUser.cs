using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskManagement.Api.Auth;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var subject = ReadSubject(httpContextAccessor.HttpContext?.User);
            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new InvalidOperationException(
                    $"The authenticated user's '{JwtRegisteredClaimNames.Sub}' claim must be a Guid; got '{subject}'.");
        }
    }

    public static string? ReadSubject(ClaimsPrincipal? principal) =>
        principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
