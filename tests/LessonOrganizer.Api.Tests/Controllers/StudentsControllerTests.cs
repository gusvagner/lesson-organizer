using LessonOrganizer.Api.Controllers;
using LessonOrganizer.Application.Students;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Testing.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Tests.Controllers;

public class StudentsControllerTests
{
    private static readonly Guid TeacherA = Guid.CreateVersion7();
    private static readonly Guid TeacherB = Guid.CreateVersion7();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly InMemoryData _data = new();

    [Fact]
    public async Task Create_Valid_Returns201WithLocationRouteId()
    {
        var result = await ControllerFor(TeacherA).Create(Request("Ana"), Ct);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var student = Assert.IsType<StudentResponse>(created.Value);
        Assert.Equal((nameof(StudentsController.Get), (object?)student.Id), (created.ActionName, created.RouteValues!["id"]));
    }

    [Fact]
    public async Task Create_Invalid_Returns400WithFieldErrors()
    {
        var result = await ControllerFor(TeacherA).Create(new StudentRequest("", "ana@", EnglishLevel.Beginner, null), Ct);
        var problem = Assert.IsType<ValidationProblemDetails>(Assert.IsType<ObjectResult>(result).Value);
        Assert.Equal(["email", "name"], problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task Get_OtherTeachersStudent_Returns404()
    {
        var id = await CreateAsync(TeacherA, "Ana");
        Assert.Equal(404, Assert.IsType<ObjectResult>(await ControllerFor(TeacherB).Get(id, Ct)).StatusCode);
    }

    [Fact]
    public async Task Update_Valid_Returns200()
    {
        var id = await CreateAsync(TeacherA, "Ana");
        var result = await ControllerFor(TeacherA).Update(id, Request("Ana Maria"), Ct);
        Assert.Equal("Ana Maria", Assert.IsType<StudentResponse>(Assert.IsType<OkObjectResult>(result).Value).Name);
    }

    [Fact]
    public async Task Delete_WithLessonPlans_Returns409()
    {
        var id = await CreateAsync(TeacherA, "Ana");
        await new InMemoryLessonPlanRepository(_data).AddAsync(LessonPlan.Create(id, "Review", null, new DateOnly(2026, 11, 12)).Value, Ct);
        Assert.Equal(409, Assert.IsType<ObjectResult>(await ControllerFor(TeacherA).Delete(id, Ct)).StatusCode);
    }

    [Fact]
    public async Task Delete_NoPlans_Returns204()
    {
        var id = await CreateAsync(TeacherA, "Ana");
        Assert.IsType<NoContentResult>(await ControllerFor(TeacherA).Delete(id, Ct));
    }

    [Fact]
    public async Task List_ReturnsOwnStudents()
    {
        await CreateAsync(TeacherA, "Ana");
        await CreateAsync(TeacherB, "Bob");
        var result = await ControllerFor(TeacherA).List(null, Ct);
        var list = Assert.IsType<IReadOnlyList<StudentResponse>>(Assert.IsType<OkObjectResult>(result).Value, exactMatch: false);
        Assert.Equal(["Ana"], list.Select(student => student.Name));
    }

    private StudentsController ControllerFor(Guid teacherId) =>
        new(new StudentService(new InMemoryStudentRepository(_data), new InMemoryLessonPlanRepository(_data), new FixedCurrentTeacher(teacherId)));

    private static StudentRequest Request(string name) => new(name, null, EnglishLevel.Intermediate, null);

    private async Task<Guid> CreateAsync(Guid teacherId, string name)
    {
        var created = await ControllerFor(teacherId).Create(Request(name), Ct);
        return Assert.IsType<StudentResponse>(Assert.IsType<CreatedAtActionResult>(created).Value).Id;
    }
}
