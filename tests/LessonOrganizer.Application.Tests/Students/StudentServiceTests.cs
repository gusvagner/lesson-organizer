using LessonOrganizer.Application.Students;
using LessonOrganizer.Application.Tests.Fakes;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Tests.Students;

public class StudentServiceTests
{
    private static readonly Guid TeacherA = Guid.CreateVersion7();
    private static readonly Guid TeacherB = Guid.CreateVersion7();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly InMemoryData _data = new();
    private readonly InMemoryStudentRepository _students;
    private readonly InMemoryLessonPlanRepository _lessonPlans;

    public StudentServiceTests()
    {
        _students = new InMemoryStudentRepository(_data);
        _lessonPlans = new InMemoryLessonPlanRepository(_data);
    }

    [Fact]
    public async Task Delete_StudentHasLessonPlans_ReturnsConflictAndKeepsStudent()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        await SeedLessonPlanAsync(ana.Id, "Present perfect");
        var result = await ServiceFor(TeacherA).DeleteAsync(ana.Id, Ct);
        Assert.Equal(
            Error.Conflict("This student has lesson plans. Delete them first or move them to another student."),
            Assert.Single(result.Errors));
        Assert.NotNull(await _students.GetOwnedAsync(ana.Id, TeacherA, Ct));
    }

    [Fact]
    public async Task Get_StudentOfAnotherTeacher_ReturnsNotFound()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherB).GetAsync(ana.Id, Ct);
        Assert.Equal(Error.NotFound("Student not found."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Create_ValidRequest_SetsTeacherFromCurrentTeacher()
    {
        var result = await ServiceFor(TeacherA).CreateAsync(Request("Ana"), Ct);
        var stored = await _students.GetOwnedAsync(result.Value.Id, TeacherA, Ct);
        Assert.NotNull(stored);
        Assert.Equal("Ana", result.Value.Name);
        Assert.Equal(0, result.Value.LessonPlanCount);
    }

    [Fact]
    public async Task Create_MissingEnglishLevel_ReturnsEnglishLevelRequired()
    {
        var result = await ServiceFor(TeacherA).CreateAsync(new StudentRequest("Ana", null, null, null), Ct);
        Assert.Equal(Error.Validation("englishLevel", "English level is required."), Assert.Single(result.Errors));
        Assert.Empty(_data.Students);
    }

    [Fact]
    public async Task Create_InvalidData_ReturnsDomainErrorsAndStoresNothing()
    {
        var result = await ServiceFor(TeacherA).CreateAsync(new StudentRequest(" ", "ana@", null, null), Ct);
        Assert.Equal(["englishLevel", "name", "email"], result.Errors.Select(error => error.Field));
        Assert.Empty(_data.Students);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnStudentsSortedByName()
    {
        await SeedStudentAsync(TeacherA, "Carla");
        await SeedStudentAsync(TeacherA, "ana");
        await SeedStudentAsync(TeacherB, "Bob");
        var list = await ServiceFor(TeacherA).ListAsync(null, Ct);
        Assert.Equal(["ana", "Carla"], list.Select(student => student.Name));
    }

    [Fact]
    public async Task List_SearchIgnoresCase()
    {
        await SeedStudentAsync(TeacherA, "Ana");
        await SeedStudentAsync(TeacherA, "Mariana");
        await SeedStudentAsync(TeacherA, "Bruno");
        var list = await ServiceFor(TeacherA).ListAsync("ANA", Ct);
        Assert.Equal(["Ana", "Mariana"], list.Select(student => student.Name));
    }

    [Fact]
    public async Task List_BlankSearch_ReturnsAll()
    {
        await SeedStudentAsync(TeacherA, "Ana");
        await SeedStudentAsync(TeacherA, "Bruno");
        Assert.Equal(2, (await ServiceFor(TeacherA).ListAsync("   ", Ct)).Count);
    }

    [Fact]
    public async Task List_IncludesLessonPlanCount()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        await SeedStudentAsync(TeacherA, "Bruno");
        await SeedLessonPlanAsync(ana.Id, "One");
        await SeedLessonPlanAsync(ana.Id, "Two");
        var list = await ServiceFor(TeacherA).ListAsync(null, Ct);
        Assert.Equal([2, 0], list.Select(student => student.LessonPlanCount));
    }

    [Fact]
    public async Task Update_StudentOfAnotherTeacher_ReturnsNotFound()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherB).UpdateAsync(ana.Id, Request("Hacked"), Ct);
        Assert.Equal(Error.NotFound("Student not found."), Assert.Single(result.Errors));
        Assert.Equal("Ana", _data.Students.Single().Name);
    }

    [Fact]
    public async Task Update_InvalidData_ReturnsValidationAndKeepsStored()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherA).UpdateAsync(ana.Id, new StudentRequest("", null, EnglishLevel.Advanced, null), Ct);
        Assert.Equal(ErrorKind.Validation, Assert.Single(result.Errors).Kind);
        var stored = await _students.GetOwnedAsync(ana.Id, TeacherA, Ct);
        Assert.Equal("Ana", stored!.Name);
        Assert.Equal(EnglishLevel.Intermediate, stored.EnglishLevel);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsUpdatedStudent()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherA).UpdateAsync(ana.Id, new StudentRequest("Ana Maria", "ana@example.com", EnglishLevel.Advanced, "Notes"), Ct);
        Assert.Equal("Ana Maria", result.Value.Name);
        Assert.Equal("ana@example.com", result.Value.Email);
        Assert.Equal("Ana Maria", _data.Students.Single().Name);
    }

    [Fact]
    public async Task Delete_StudentOfAnotherTeacher_ReturnsNotFound()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherB).DeleteAsync(ana.Id, Ct);
        Assert.Equal(Error.NotFound("Student not found."), Assert.Single(result.Errors));
        Assert.Single(_data.Students);
    }

    [Fact]
    public async Task Delete_NoLessonPlans_Removes()
    {
        var ana = await SeedStudentAsync(TeacherA, "Ana");
        var result = await ServiceFor(TeacherA).DeleteAsync(ana.Id, Ct);
        Assert.True(result.IsSuccess);
        Assert.Empty(_data.Students);
    }

    [Fact]
    public async Task Create_DuplicateNameAndEmail_IsAccepted()
    {
        var service = ServiceFor(TeacherA);
        var request = new StudentRequest("Ana", "ana@example.com", EnglishLevel.Beginner, null);
        Assert.True((await service.CreateAsync(request, Ct)).IsSuccess);
        Assert.True((await service.CreateAsync(request, Ct)).IsSuccess);
        Assert.Equal(2, _data.Students.Count);
    }

    private StudentService ServiceFor(Guid teacherId) =>
        new(_students, _lessonPlans, new FixedCurrentTeacher(teacherId));

    private static StudentRequest Request(string name) => new(name, null, EnglishLevel.Intermediate, null);

    private async Task<Student> SeedStudentAsync(Guid teacherId, string name)
    {
        var student = Student.Create(teacherId, name, null, EnglishLevel.Intermediate, null).Value;
        await _students.AddAsync(student, Ct);
        return student;
    }

    private async Task SeedLessonPlanAsync(Guid studentId, string title)
    {
        var plan = LessonPlan.Create(studentId, title, null, DateOnly.FromDateTime(DateTime.Today)).Value;
        await _lessonPlans.AddAsync(plan, Ct);
    }
}
