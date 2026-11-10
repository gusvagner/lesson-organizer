using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Application.Tests.Fakes;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Tests.LessonPlans;

public class LessonPlanServiceTests
{
    private static readonly Guid TeacherA = Guid.CreateVersion7();
    private static readonly Guid TeacherB = Guid.CreateVersion7();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Error StudentNotFound = Error.Validation("studentId", "Student not found.");
    private static readonly Error PlanNotFound = Error.NotFound("Lesson plan not found.");

    private readonly InMemoryData _data = new();
    private readonly InMemoryStudentRepository _students;
    private readonly InMemoryLessonPlanRepository _lessonPlans;

    public LessonPlanServiceTests()
    {
        _students = new InMemoryStudentRepository(_data);
        _lessonPlans = new InMemoryLessonPlanRepository(_data);
    }

    [Fact]
    public async Task Create_StudentOfAnotherTeacher_ReturnsSameErrorAsMissingStudent()
    {
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var foreign = await ServiceFor(TeacherA).CreateAsync(Request(bStudent.Id), Ct);
        var missing = await ServiceFor(TeacherA).CreateAsync(Request(Guid.CreateVersion7()), Ct);
        Assert.Equal(StudentNotFound, Assert.Single(foreign.Errors));
        Assert.Equal(foreign.Errors, missing.Errors);
        Assert.Empty(_data.LessonPlans);
    }

    [Fact]
    public async Task List_FilterByAnotherTeachersStudent_ReturnsEmpty()
    {
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        await SeedLessonPlanAsync(bStudent.Id, "B's plan");
        var list = await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(bStudent.Id, null), Ct);
        Assert.Empty(list);
    }

    [Fact]
    public async Task Create_ValidRequest_StartsAsToPrepareWithStudentName()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherA).CreateAsync(Request(ana.Id), Ct);
        Assert.Equal(LessonPlanStatus.ToPrepare, result.Value.Status);
        Assert.Equal("Ana", result.Value.StudentName);
        Assert.Single(_data.LessonPlans);
    }

    [Fact]
    public async Task Create_MissingDate_ReturnsLessonDateRequired()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherA).CreateAsync(new LessonPlanRequest(ana.Id, "Review", null, null), Ct);
        Assert.Equal(Error.Validation("lessonDate", "Lesson date is required."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Create_MissingStudentId_ReturnsStudentNotFound()
    {
        var result = await ServiceFor(TeacherA).CreateAsync(new LessonPlanRequest(null, "Review", null, Today), Ct);
        Assert.Equal(StudentNotFound, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Create_InvalidTitleAndForeignStudent_ReturnsBothErrors()
    {
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var result = await ServiceFor(TeacherA).CreateAsync(new LessonPlanRequest(bStudent.Id, " ", null, Today), Ct);
        Assert.Equal(["studentId", "title"], result.Errors.Select(error => error.Field));
    }

    [Fact]
    public async Task Update_MoveToAnotherOwnStudent_Succeeds()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var bruno = await SeedStudentAsync(TeacherA, "Bruno");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, Request(bruno.Id), Ct);
        Assert.Equal("Bruno", result.Value.StudentName);
        Assert.Equal(bruno.Id, plan.StudentId);
    }

    [Fact]
    public async Task Update_MoveToForeignStudent_ReturnsStudentNotFound()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, Request(bStudent.Id), Ct);
        Assert.Equal(StudentNotFound, Assert.Single(result.Errors));
        Assert.Equal(ana.Id, plan.StudentId);
    }

    [Fact]
    public async Task Update_DoesNotChangeStatus()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        plan.ChangeStatus(LessonPlanStatus.Ready);
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, Request(ana.Id, "New title"), Ct);
        Assert.Equal(LessonPlanStatus.Ready, result.Value.Status);
        Assert.Equal("New title", result.Value.Title);
    }

    [Fact]
    public async Task Update_PlanOfAnotherTeacher_ReturnsNotFound()
    {
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var plan = await SeedLessonPlanAsync(bStudent.Id, "Review");
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, Request(bStudent.Id), Ct);
        Assert.Equal(PlanNotFound, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task ChangeStatus_AnyToAny_Succeeds()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var service = ServiceFor(TeacherA);
        await service.ChangeStatusAsync(plan.Id, new ChangeLessonPlanStatusRequest(LessonPlanStatus.Delivered), Ct);
        var result = await service.ChangeStatusAsync(plan.Id, new ChangeLessonPlanStatusRequest(LessonPlanStatus.ToPrepare), Ct);
        Assert.Equal(LessonPlanStatus.ToPrepare, result.Value.Status);
    }

    [Fact]
    public async Task ChangeStatus_MissingStatus_ReturnsStatusRequired()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var result = await ServiceFor(TeacherA).ChangeStatusAsync(plan.Id, new ChangeLessonPlanStatusRequest(null), Ct);
        Assert.Equal(Error.Validation("status", "Status is required."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task ChangeStatus_PlanOfAnotherTeacher_ReturnsNotFound()
    {
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var plan = await SeedLessonPlanAsync(bStudent.Id, "Review");
        var result = await ServiceFor(TeacherA).ChangeStatusAsync(plan.Id, new ChangeLessonPlanStatusRequest(LessonPlanStatus.Ready), Ct);
        Assert.Equal(PlanNotFound, Assert.Single(result.Errors));
        Assert.Equal(LessonPlanStatus.ToPrepare, plan.Status);
    }

    [Fact]
    public async Task List_SortedByDateThenTitle()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        await SeedLessonPlanAsync(ana.Id, "b later", Today.AddDays(2));
        await SeedLessonPlanAsync(ana.Id, "B today", Today);
        await SeedLessonPlanAsync(ana.Id, "a today", Today);
        var list = await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(null, null), Ct);
        Assert.Equal(["a today", "B today", "b later"], list.Select(plan => plan.Title));
    }

    [Fact]
    public async Task List_StudentAndStatusFiltersCombine()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var bruno = await SeedStudentAsync(TeacherA, "Bruno");
        var ready = await SeedLessonPlanAsync(ana.Id, "Ana ready");
        ready.ChangeStatus(LessonPlanStatus.Ready);
        await SeedLessonPlanAsync(ana.Id, "Ana to prepare");
        var brunoReady = await SeedLessonPlanAsync(bruno.Id, "Bruno ready");
        brunoReady.ChangeStatus(LessonPlanStatus.Ready);
        var list = await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(ana.Id, LessonPlanStatus.Ready), Ct);
        Assert.Equal(["Ana ready"], list.Select(plan => plan.Title));
    }

    [Fact]
    public async Task Get_OwnPlan_ReturnsWithStudentName()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var result = await ServiceFor(TeacherA).GetAsync(plan.Id, Ct);
        Assert.Equal("Ana", result.Value.StudentName);
    }

    [Fact]
    public async Task Delete_PlanOfAnotherTeacher_ReturnsNotFound()
    {
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var plan = await SeedLessonPlanAsync(bStudent.Id, "Review");
        var result = await ServiceFor(TeacherA).DeleteAsync(plan.Id, Ct);
        Assert.Equal(PlanNotFound, Assert.Single(result.Errors));
        Assert.Single(_data.LessonPlans);
    }

    [Fact]
    public async Task Delete_OwnPlan_Removes()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var result = await ServiceFor(TeacherA).DeleteAsync(plan.Id, Ct);
        Assert.True(result.IsSuccess);
        Assert.Empty(_data.LessonPlans);
    }

    private LessonPlanService ServiceFor(Guid teacherId) =>
        new(_lessonPlans, _students, new FixedCurrentTeacher(teacherId));

    private static LessonPlanRequest Request(Guid? studentId, string title = "Present perfect") =>
        new(studentId, title, null, Today);

    private async Task<Student> SeedStudentAsync(Guid teacherId, string name)
    {
        var student = Student.Create(teacherId, name, null, EnglishLevel.Intermediate, null).Value;
        await _students.AddAsync(student, Ct);
        return student;
    }

    private async Task<LessonPlan> SeedLessonPlanAsync(Guid studentId, string title, DateOnly? date = null)
    {
        var plan = LessonPlan.Create(studentId, title, null, date ?? Today).Value;
        await _lessonPlans.AddAsync(plan, Ct);
        return plan;
    }
}
