using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Infrastructure.Identity;
using LessonOrganizer.Infrastructure.Persistence;
using LessonOrganizer.Infrastructure.Repositories;
using LessonOrganizer.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LessonOrganizer.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LessonOrganizerDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<ILessonPlanRepository, EfLessonPlanRepository>();
        services.AddIdentityCore<ApplicationUser>(ConfigureIdentity).AddEntityFrameworkStores<LessonOrganizerDbContext>();
        services.AddScoped<IUserAccounts, IdentityUserAccounts>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<DemoDataSeeder>();
        return services;
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "";
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;
        options.Lockout.MaxFailedAccessAttempts = MaxFailedLoginAttempts;
        options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
        options.Lockout.AllowedForNewUsers = true;
    }
}
