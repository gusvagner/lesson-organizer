using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Infrastructure.Seed;
using LessonOrganizer.Infrastructure.Tests.Persistence;
using LessonOrganizer.Testing;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LessonOrganizer.Infrastructure.Tests.Seed;

public sealed class DemoDataSeederTests(SqlServerFixture sql) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private TestDatabase _db = null!;
    private ServiceProvider _services = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _db = await TestDatabase.CreateAsync(sql);
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(new FixedTimeProvider(Now))
            .AddInfrastructure(_db.ConnectionString)
            .BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task Seed_EmptyDatabase_CreatesPlanMdData()
    {
        Assert.True(await SeedAsync());
        await using var context = _db.NewContext();
        Assert.Equal(2, await context.Users.CountAsync(Ct));
        Assert.Equal(5, await context.Students.CountAsync(Ct));
        var plans = await context.LessonPlans.OrderBy(plan => plan.LessonDate).ToListAsync(Ct);
        Assert.Equal(new DateOnly(2026, 11, 3), plans[0].LessonDate);
        Assert.Equal(
            [LessonPlanStatus.ToPrepare, LessonPlanStatus.ToPrepare, LessonPlanStatus.Ready, LessonPlanStatus.Ready, LessonPlanStatus.Delivered, LessonPlanStatus.Delivered],
            plans.Select(plan => plan.Status).Order());
    }

    [Fact]
    public async Task Seed_AnaHasDeliveredAndReadyPlans()
    {
        await SeedAsync();
        await using var context = _db.NewContext();
        var ana = await context.Students.SingleAsync(student => student.Name == "Ana", Ct);
        var plans = await context.LessonPlans.Where(plan => plan.StudentId == ana.Id).OrderBy(plan => plan.LessonDate).ToListAsync(Ct);
        Assert.Equal(
            [("Present perfect — conversation", LessonPlanStatus.Delivered, new DateOnly(2026, 11, 3)), ("Phrasal verbs at work", LessonPlanStatus.Ready, new DateOnly(2026, 11, 12))],
            plans.Select(plan => (plan.Title, plan.Status, plan.LessonDate)));
    }

    [Fact]
    public async Task Seed_SecondRun_ReturnsFalseAndAddsNothing()
    {
        await SeedAsync();
        Assert.False(await SeedAsync());
        await using var context = _db.NewContext();
        Assert.Equal((2, 5, 6), (await context.Users.CountAsync(Ct), await context.Students.CountAsync(Ct), await context.LessonPlans.CountAsync(Ct)));
    }

    [Fact]
    public async Task Seed_DatabaseWithAnyUser_AddsNothing()
    {
        await _db.AddTeacherAsync("someone@example.com");
        Assert.False(await SeedAsync());
        await using var context = _db.NewContext();
        Assert.Equal(0, await context.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task SeededUsers_CanLogInWithDemoPassword()
    {
        await SeedAsync();
        await using var scope = _services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccounts>();
        Assert.Equal(LoginStatus.Succeeded, (await accounts.CheckPasswordAsync("maria@example.com", "Demo1234!", Ct)).Status);
        Assert.Equal(LoginStatus.Succeeded, (await accounts.CheckPasswordAsync("john@example.com", "Demo1234!", Ct)).Status);
    }

    [Fact]
    public async Task Seed_CarlaHasNoPlans()
    {
        await SeedAsync();
        await using var context = _db.NewContext();
        var carla = await context.Students.SingleAsync(student => student.Name == "Carla", Ct);
        Assert.False(await context.LessonPlans.AnyAsync(plan => plan.StudentId == carla.Id, Ct));
    }

    private async Task<bool> SeedAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(Ct);
    }
}
