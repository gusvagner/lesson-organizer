using TaskManagement.Api.Auth;
using TaskManagement.Api.Data;
using TaskManagement.Api.Errors;
using TaskManagement.Api.Http;
using TaskManagement.Api.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddJwtAuthentication();
builder.Services.AddPersistence();
builder.Services.AddTasks();
builder.Services.AddApiControllers();
builder.Services.AddApiProblemDetails();

var app = builder.Build();

app.UseApiErrorHandling();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
