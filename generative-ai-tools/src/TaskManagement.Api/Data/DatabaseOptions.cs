namespace TaskManagement.Api.Data;

public sealed class DatabaseOptions
{
    public const string ConnectionStringName = "TaskManagement";
    public const string ConnectionStringKey = $"ConnectionStrings:{ConnectionStringName}";

    public string ConnectionString { get; set; } = string.Empty;
}
