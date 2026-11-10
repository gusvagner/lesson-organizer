using LessonOrganizer.Api.Startup;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddJsonConsole();
builder.Services.AddApi(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}
else
{
    app.UseExceptionHandler();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DatabaseStartup.MigrateIfConfiguredAsync(app);
await app.RunAsync();
