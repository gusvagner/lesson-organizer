using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskManagement.Api.Auth;

public static class JwtSetup
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddValidatedJwtOptions();
        services.AddScoped<SubjectValidatingJwtEvents>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) => ConfigureBearer(bearer, jwt.Value));
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    public static IServiceCollection AddValidatedJwtOptions(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>());
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateOnStart();
        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, JwtOptions jwt)
    {
        bearer.MapInboundClaims = false;
        bearer.IncludeErrorDetails = false;
        bearer.EventsType = typeof(SubjectValidatingJwtEvents);
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = AllowedClockSkew,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    }
}
