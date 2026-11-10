using LessonOrganizer.Api.Http;
using LessonOrganizer.Api.Security;
using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LessonOrganizer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, IAuthSession session, ICurrentTeacher currentTeacher) : ControllerBase
{
    public const string XsrfCookieName = "XSRF-TOKEN";

    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(XsrfCookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
        return NoContent();
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, ct);
        return result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created }
            : ResultHttpMapping.ToProblem(result.Errors);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        if (result.IsSuccess)
        {
            await session.SignInAsync(result.Value.Id);
        }

        return ResultHttpMapping.ToOk(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await session.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await auth.GetAsync(currentTeacher.Id, ct);
        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ResultHttpMapping.ToProblem([Error.Unauthorized("Your session is no longer valid. Log in again.")]);
    }
}
