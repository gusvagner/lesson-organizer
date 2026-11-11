namespace TaskManagement.Api.Tasks;

public static class TasksSetup
{
    public static IServiceCollection AddTasks(this IServiceCollection services) => services.AddScoped<TaskService>();
}
