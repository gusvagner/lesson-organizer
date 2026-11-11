using LessonOrganizer.Api.Http;
using LessonOrganizer.Api.Startup;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddJsonConsole();
builder.Services.AddApi(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseSecurityHeaders();
app.UseRequestBodyLimit();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}
else
{
    app.UseExceptionHandler();
}

app.UseSpaStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSpaFallback();

await DatabaseStartup.MigrateIfConfiguredAsync(app);
await app.RunAsync();
