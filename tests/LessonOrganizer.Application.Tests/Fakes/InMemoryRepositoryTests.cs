using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Tests.Fakes;

public class InMemoryRepositoryTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Guid TeacherId = Guid.CreateVersion7();

    private readonly InMemoryData _data = new();

    [Fact]
    public async Task StudentGetOwned_ChangedWithoutUpdate_StoredStaysTheSame()
    {
        var students = new InMemoryStudentRepository(_data);
        var ana = Student.Create(TeacherId, "Ana", null, EnglishLevel.Beginner, null).Value;
        await students.AddAsync(ana, Ct);
        ana.Update("Changed after add", null, EnglishLevel.Beginner, null);
        var loaded = await students.GetOwnedAsync(ana.Id, TeacherId, Ct);
        loaded!.Update("Changed after get", null, EnglishLevel.Beginner, null);
        Assert.Equal("Ana", (await students.GetOwnedAsync(ana.Id, TeacherId, Ct))!.Name);
    }

    [Fact]
    public async Task StudentUpdate_SavesChanges()
    {
        var students = new InMemoryStudentRepository(_data);
        var ana = Student.Create(TeacherId, "Ana", null, EnglishLevel.Beginner, null).Value;
        await students.AddAsync(ana, Ct);
        var loaded = await students.GetOwnedAsync(ana.Id, TeacherId, Ct);
        loaded!.Update("Ana Maria", null, EnglishLevel.Beginner, null);
        await students.UpdateAsync(loaded, Ct);
        Assert.Equal("Ana Maria", (await students.GetOwnedAsync(ana.Id, TeacherId, Ct))!.Name);
    }

    [Fact]
    public async Task LessonPlanGetOwned_ChangedWithoutUpdate_StoredStaysTheSame()
    {
        var plan = await SeedPlanAsync();
        var lessonPlans = new InMemoryLessonPlanRepository(_data);
        plan.ChangeStatus(LessonPlanStatus.Delivered);
        var loaded = await lessonPlans.GetOwnedAsync(plan.Id, TeacherId, Ct);
        loaded!.ChangeStatus(LessonPlanStatus.Ready);
        Assert.Equal(LessonPlanStatus.ToPrepare, (await lessonPlans.GetOwnedAsync(plan.Id, TeacherId, Ct))!.Status);
    }

    [Fact]
    public async Task LessonPlanUpdate_SavesChanges()
    {
        var plan = await SeedPlanAsync();
        var lessonPlans = new InMemoryLessonPlanRepository(_data);
        var loaded = await lessonPlans.GetOwnedAsync(plan.Id, TeacherId, Ct);
        loaded!.ChangeStatus(LessonPlanStatus.Ready);
        await lessonPlans.UpdateAsync(loaded, Ct);
        Assert.Equal(LessonPlanStatus.Ready, (await lessonPlans.GetOwnedAsync(plan.Id, TeacherId, Ct))!.Status);
    }

    private async Task<LessonPlan> SeedPlanAsync()
    {
        var ana = Student.Create(TeacherId, "Ana", null, EnglishLevel.Beginner, null).Value;
        await new InMemoryStudentRepository(_data).AddAsync(ana, Ct);
        var plan = LessonPlan.Create(ana.Id, "Review", null, new DateOnly(2026, 11, 12)).Value;
        await new InMemoryLessonPlanRepository(_data).AddAsync(plan, Ct);
        return plan;
    }
}
