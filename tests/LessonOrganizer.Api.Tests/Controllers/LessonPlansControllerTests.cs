using LessonOrganizer.Api.Controllers;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Testing.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Tests.Controllers;

public class LessonPlansControllerTests
{
    private static readonly Guid TeacherA = Guid.CreateVersion7();
    private static readonly Guid TeacherB = Guid.CreateVersion7();
    private static readonly DateOnly Today = new(2026, 11, 12);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly InMemoryData _data = new();

    [Fact]
    public async Task Create_ForeignStudent_Returns400OnStudentId()
    {
        var bStudent = await SeedStudentAsync(TeacherB);
        var result = await ControllerFor(TeacherA).Create(new LessonPlanRequest(bStudent, "Review", null, Today), Ct);
        var problem = Assert.IsType<ValidationProblemDetails>(Assert.IsType<ObjectResult>(result).Value);
        Assert.Equal(["Student not found."], problem.Errors["studentId"]);
    }

    [Fact]
    public async Task Create_Valid_Returns201WithLocationRouteId()
    {
        var student = await SeedStudentAsync(TeacherA);
        var created = Assert.IsType<CreatedAtActionResult>(await ControllerFor(TeacherA).Create(new LessonPlanRequest(student, "Review", null, Today), Ct));
        Assert.Equal(nameof(LessonPlansController.Get), created.ActionName);
        Assert.Equal(LessonPlanStatus.ToPrepare, Assert.IsType<LessonPlanResponse>(created.Value).Status);
    }

    [Fact]
    public async Task ChangeStatus_Valid_Returns200WithNewStatus()
    {
        var plan = await SeedPlanAsync(TeacherA);
        var result = await ControllerFor(TeacherA).ChangeStatus(plan, new ChangeLessonPlanStatusRequest(LessonPlanStatus.Delivered), Ct);
        Assert.Equal(LessonPlanStatus.Delivered, Assert.IsType<LessonPlanResponse>(Assert.IsType<OkObjectResult>(result).Value).Status);
    }

    [Fact]
    public async Task ChangeStatus_OtherTeachersPlan_Returns404()
    {
        var plan = await SeedPlanAsync(TeacherA);
        var result = await ControllerFor(TeacherB).ChangeStatus(plan, new ChangeLessonPlanStatusRequest(LessonPlanStatus.Ready), Ct);
        Assert.Equal(404, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task List_UndefinedStatus_Returns400()
    {
        var result = await ControllerFor(TeacherA).List(null, (LessonPlanStatus)7, Ct);
        Assert.Equal(400, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task List_Own_Returns200()
    {
        await SeedPlanAsync(TeacherA);
        await SeedPlanAsync(TeacherB);
        var result = await ControllerFor(TeacherA).List(null, null, Ct);
        Assert.Single(Assert.IsType<IReadOnlyList<LessonPlanResponse>>(Assert.IsType<OkObjectResult>(result).Value, exactMatch: false));
    }

    [Fact]
    public async Task Delete_Own_Returns204()
    {
        var plan = await SeedPlanAsync(TeacherA);
        Assert.IsType<NoContentResult>(await ControllerFor(TeacherA).Delete(plan, Ct));
    }

    private LessonPlansController ControllerFor(Guid teacherId) =>
        new(new LessonPlanService(new InMemoryLessonPlanRepository(_data), new InMemoryStudentRepository(_data), new FixedCurrentTeacher(teacherId)));

    private async Task<Guid> SeedStudentAsync(Guid teacherId)
    {
        var student = Student.Create(teacherId, "Ana", null, EnglishLevel.Beginner, null).Value;
        await new InMemoryStudentRepository(_data).AddAsync(student, Ct);
        return student.Id;
    }

    private async Task<Guid> SeedPlanAsync(Guid teacherId)
    {
        var plan = LessonPlan.Create(await SeedStudentAsync(teacherId), "Review", null, Today).Value;
        await new InMemoryLessonPlanRepository(_data).AddAsync(plan, Ct);
        return plan.Id;
    }
}
