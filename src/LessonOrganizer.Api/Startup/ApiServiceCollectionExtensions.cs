using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LessonOrganizer.Api.Http;
using LessonOrganizer.Api.Security;
using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Application.Students;
using LessonOrganizer.Infrastructure;
using LessonOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Startup;

public static class ApiServiceCollectionExtensions
{
    public const string ConnectionStringName = "LessonOrganizer";
    public const string XsrfHeaderName = "X-XSRF-TOKEN";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(RequiredConnectionString(configuration));
        services.AddScoped<AuthService>().AddScoped<StudentService>().AddScoped<LessonPlanService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTeacher, ClaimsCurrentTeacher>();
        services.AddScoped<IAuthSession, IdentityAuthSession>();
        AddCookieAuthentication(services);
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        AddAntiforgery(services);
        AddControllers(services);
        services.AddProblemDetails();
        AddAuthRateLimiter(services, configuration);
        return services;
    }

    private static string RequiredConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName)
        ?? throw new InvalidOperationException(
            $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings:{ConnectionStringName} (user secrets in development, environment variable in Docker).");

    private static void AddCookieAuthentication(IServiceCollection services)
    {
        new IdentityBuilder(typeof(ApplicationUser), services).AddSignInManager();
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = SessionLifetime;
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context => WriteStatus(context.Response, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => WriteStatus(context.Response, StatusCodes.Status403Forbidden);
        });
    }

    private static Task WriteStatus(HttpResponse response, int status)
    {
        response.StatusCode = status;
        return Task.CompletedTask;
    }

    private static void AddAntiforgery(IServiceCollection services) =>
        services.AddAntiforgery(options =>
        {
            options.HeaderName = XsrfHeaderName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

    private static void AddControllers(IServiceCollection services) =>
        services
            .AddControllers(options => options.Filters.Add<AntiforgeryValidationFilter>())
            .AddJsonOptions(options =>
            {
                options.AllowInputFormatterExceptionMessages = false;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            })
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidInputResponse.Create);

    private static void AddAuthRateLimiter(IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window }));
        });
    }
}
