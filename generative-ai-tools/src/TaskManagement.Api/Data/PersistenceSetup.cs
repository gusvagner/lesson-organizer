using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManagement.Api.Tasks;
using TaskManagement.Api.Users;

namespace TaskManagement.Api.Data;

public static class PersistenceSetup
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>(BindConnectionString)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"{DatabaseOptions.ConnectionStringKey} is required; got an empty or missing value.")
            .ValidateOnStart();
        services.AddDbContext<TaskManagementDbContext>((provider, builder) =>
            builder.UseSqlServer(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        return services;
    }

    private static void BindConnectionString(DatabaseOptions options, IConfiguration configuration) =>
        options.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty;
}
