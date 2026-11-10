using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LessonOrganizer.Infrastructure.Tests;

public class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_RegistersScopedRepositories()
    {
        using var provider = new ServiceCollection()
            .AddInfrastructure("Server=localhost;Database=unused;TrustServerCertificate=true")
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        Assert.IsType<EfStudentRepository>(scope.ServiceProvider.GetRequiredService<IStudentRepository>());
        Assert.IsType<EfLessonPlanRepository>(scope.ServiceProvider.GetRequiredService<ILessonPlanRepository>());
    }
}
