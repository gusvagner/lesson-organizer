using TaskManagement.Api.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatedJwtOptions();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program;
