using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Infrastructure.Persistence;
using LessonOrganizer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LessonOrganizer.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LessonOrganizerDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<ILessonPlanRepository, EfLessonPlanRepository>();
        return services;
    }
}
