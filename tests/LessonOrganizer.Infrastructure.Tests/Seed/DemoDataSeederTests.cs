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
        Assert.Equal(7, await context.Students.CountAsync(Ct));
        var plans = await context.LessonPlans.OrderBy(plan => plan.LessonDate).ToListAsync(Ct);
        Assert.Equal(new DateOnly(2026, 10, 31), plans[0].LessonDate);
        Assert.Equal(
            (4, 3, 4),
            (plans.Count(plan => plan.Status == LessonPlanStatus.ToPrepare),
             plans.Count(plan => plan.Status == LessonPlanStatus.Ready),
             plans.Count(plan => plan.Status == LessonPlanStatus.Delivered)));
    }

    [Fact]
    public async Task Seed_AnaHasDeliveredAndReadyPlans()
    {
        await SeedAsync();
        await using var context = _db.NewContext();
        var ana = await context.Students.SingleAsync(student => student.Name == "Ana Souza", Ct);
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
        Assert.Equal((2, 7, 11), (await context.Users.CountAsync(Ct), await context.Students.CountAsync(Ct), await context.LessonPlans.CountAsync(Ct)));
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
        var carla = await context.Students.SingleAsync(student => student.Name == "Carla Mendes", Ct);
        Assert.False(await context.LessonPlans.AnyAsync(plan => plan.StudentId == carla.Id, Ct));
    }

    [Fact]
    public async Task Seed_MariaHasFiveStudentsWithEmailAndNotes()
    {
        await SeedAsync();
        await using var context = _db.NewContext();
        var mariaId = await context.Users.Where(user => user.Email == "maria@example.com").Select(user => user.Id).SingleAsync(Ct);
        var students = await context.Students.Where(student => student.TeacherId == mariaId).OrderBy(student => student.Name).ToListAsync(Ct);
        Assert.Equal(
            ["Ana Souza", "Bruno Lima", "Carla Mendes", "Diego Alves", "Fernanda Rocha"],
            students.Select(student => student.Name));
        Assert.All(students, student => Assert.False(string.IsNullOrWhiteSpace(student.Email)));
        Assert.All(students, student => Assert.False(string.IsNullOrWhiteSpace(student.Notes)));
    }

    [Fact]
    public async Task Seed_EveryMariaPlanHasDescription()
    {
        await SeedAsync();
        await using var context = _db.NewContext();
        var mariaId = await context.Users.Where(user => user.Email == "maria@example.com").Select(user => user.Id).SingleAsync(Ct);
        var mariaStudentIds = await context.Students.Where(student => student.TeacherId == mariaId).Select(student => student.Id).ToListAsync(Ct);
        var plans = await context.LessonPlans.Where(plan => mariaStudentIds.Contains(plan.StudentId)).ToListAsync(Ct);
        Assert.Equal(8, plans.Count);
        Assert.All(plans, plan => Assert.False(string.IsNullOrWhiteSpace(plan.Description)));
    }

    private async Task<bool> SeedAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(Ct);
    }
}
