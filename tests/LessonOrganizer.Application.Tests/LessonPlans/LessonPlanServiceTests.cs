using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Testing.Fakes;
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
        var list = (await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(bStudent.Id, null), Ct)).Value;
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
        Assert.Equal(bruno.Id, StoredPlan(plan.Id).StudentId);
    }

    [Fact]
    public async Task Update_MoveToForeignStudent_ReturnsStudentNotFound()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var bStudent = await SeedStudentAsync(TeacherB, "Bob");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, Request(bStudent.Id), Ct);
        Assert.Equal(StudentNotFound, Assert.Single(result.Errors));
        Assert.Equal(ana.Id, StoredPlan(plan.Id).StudentId);
    }

    [Fact]
    public async Task Update_InvalidTitle_ReturnsErrorAndKeepsStored()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var bruno = await SeedStudentAsync(TeacherA, "Bruno");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        var request = new LessonPlanRequest(bruno.Id, new string('t', 151), "New description", Today.AddDays(5));
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, request, Ct);
        Assert.Equal("title", Assert.Single(result.Errors).Field);
        var stored = StoredPlan(plan.Id);
        Assert.Equal((ana.Id, "Review", (string?)null, Today), (stored.StudentId, stored.Title, stored.Description, stored.LessonDate));
    }

    [Fact]
    public async Task Update_DoesNotChangeStatus()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review", status: LessonPlanStatus.Ready);
        var result = await ServiceFor(TeacherA).UpdateAsync(plan.Id, Request(ana.Id, "New title"), Ct);
        Assert.Equal(LessonPlanStatus.Ready, result.Value.Status);
        Assert.Equal("New title", StoredPlan(plan.Id).Title);
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
    public async Task ChangeStatus_OwnPlan_SavesStatus()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var plan = await SeedLessonPlanAsync(ana.Id, "Review");
        await ServiceFor(TeacherA).ChangeStatusAsync(plan.Id, new ChangeLessonPlanStatusRequest(LessonPlanStatus.Delivered), Ct);
        Assert.Equal(LessonPlanStatus.Delivered, StoredPlan(plan.Id).Status);
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
        Assert.Equal(LessonPlanStatus.ToPrepare, _data.LessonPlans.Single().Status);
    }

    [Fact]
    public async Task List_SortedByDateThenTitle()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        await SeedLessonPlanAsync(ana.Id, "b later", Today.AddDays(2));
        await SeedLessonPlanAsync(ana.Id, "B today", Today);
        await SeedLessonPlanAsync(ana.Id, "a today", Today);
        var list = (await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(null, null), Ct)).Value;
        Assert.Equal(["a today", "B today", "b later"], list.Select(plan => plan.Title));
    }

    [Fact]
    public async Task List_StudentAndStatusFiltersCombine()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var bruno = await SeedStudentAsync(TeacherA, "Bruno");
        await SeedLessonPlanAsync(ana.Id, "Ana ready", status: LessonPlanStatus.Ready);
        await SeedLessonPlanAsync(ana.Id, "Ana to prepare");
        await SeedLessonPlanAsync(bruno.Id, "Bruno ready", status: LessonPlanStatus.Ready);
        var list = (await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(ana.Id, LessonPlanStatus.Ready), Ct)).Value;
        Assert.Equal(["Ana ready"], list.Select(plan => plan.Title));
    }

    [Fact]
    public async Task List_UndefinedStatus_ReturnsStatusError()
    {
        var result = await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(null, (LessonPlanStatus)7), Ct);
        Assert.Equal(Error.Validation("status", "Status must be one of: ToPrepare, Ready, Delivered."), Assert.Single(result.Errors));
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

    private async Task<LessonPlan> SeedLessonPlanAsync(
        Guid studentId, string title, DateOnly? date = null, LessonPlanStatus status = LessonPlanStatus.ToPrepare)
    {
        var plan = LessonPlan.Create(studentId, title, null, date ?? Today).Value;
        plan.ChangeStatus(status);
        await _lessonPlans.AddAsync(plan, Ct);
        return plan;
    }

    private LessonPlan StoredPlan(Guid id) => _data.LessonPlans.Single(plan => plan.Id == id);

}
