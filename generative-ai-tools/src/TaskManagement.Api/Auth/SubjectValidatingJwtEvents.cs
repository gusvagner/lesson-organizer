using Microsoft.AspNetCore.Authentication.JwtBearer;
using TaskManagement.Api.Users;

namespace TaskManagement.Api.Auth;

/// <summary>
/// Accepts a signature-valid token only when its <c>sub</c> is a Guid that names an existing user.
/// </summary>
public sealed class SubjectValidatingJwtEvents(IUserRepository users) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var subject = CurrentUser.ReadSubject(context.Principal);
        if (!Guid.TryParse(subject, out var userId))
        {
            context.Fail($"The token's 'sub' claim must be a Guid; got '{subject}'.");
            return;
        }

        if (!await users.ExistsAsync(userId, context.HttpContext.RequestAborted))
        {
            context.Fail($"The token's 'sub' claim names user {userId}, who does not exist.");
        }
    }
}
