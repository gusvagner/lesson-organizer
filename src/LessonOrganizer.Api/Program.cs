using LessonOrganizer.Api.Startup;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddJsonConsole();
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DatabaseStartup.MigrateIfConfiguredAsync(app);
await app.RunAsync();
