using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Students;

public sealed class StudentService(IStudentRepository students, ILessonPlanRepository lessonPlans, ICurrentTeacher teacher)
{
    private static readonly Error NotFound = Error.NotFound("Student not found.");
    private static readonly Error HasLessonPlans =
        Error.Conflict("This student has lesson plans. Delete them first or move them to another student.");
    private static readonly Error EnglishLevelRequired = Error.Validation("englishLevel", "English level is required.");

    public async Task<IReadOnlyList<StudentResponse>> ListAsync(string? search, CancellationToken ct)
    {
        var own = await students.ListByTeacherAsync(teacher.Id, search, ct);
        var counts = await lessonPlans.CountByStudentsAsync(own.Select(student => student.Id).ToList(), ct);
        return own.Select(student => StudentResponse.From(student, counts.GetValueOrDefault(student.Id))).ToList();
    }

    public async Task<Result<StudentResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var student = await students.GetOwnedAsync(id, teacher.Id, ct);
        return student is null ? Result<StudentResponse>.Failure(NotFound) : await ResponseAsync(student, ct);
    }

    public async Task<Result<StudentResponse>> CreateAsync(StudentRequest request, CancellationToken ct)
    {
        var created = Student.Create(teacher.Id, request.Name, request.Email, request.EnglishLevel ?? default, request.Notes);
        var errors = WithEnglishLevelCheck(request, created.Errors);
        if (errors.Count > 0)
        {
            return Result<StudentResponse>.Failure(errors);
        }

        await students.AddAsync(created.Value, ct);
        return Result<StudentResponse>.Success(StudentResponse.From(created.Value, 0));
    }

    public async Task<Result<StudentResponse>> UpdateAsync(Guid id, StudentRequest request, CancellationToken ct)
    {
        var student = await students.GetOwnedAsync(id, teacher.Id, ct);
        if (student is null)
        {
            return Result<StudentResponse>.Failure(NotFound);
        }

        var errors = WithEnglishLevelCheck(request, []);
        var updated = errors.Count == 0
            ? student.Update(request.Name, request.Email, request.EnglishLevel!.Value, request.Notes)
            : Result.Failure(errors);
        if (!updated.IsSuccess)
        {
            return Result<StudentResponse>.Failure(updated.Errors);
        }

        await students.UpdateAsync(student, ct);
        return await ResponseAsync(student, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var student = await students.GetOwnedAsync(id, teacher.Id, ct);
        if (student is null)
        {
            return Result.Failure(NotFound);
        }

        if (await LessonPlanCountAsync(student.Id, ct) > 0)
        {
            return Result.Failure(HasLessonPlans);
        }

        var deleted = await students.TryDeleteAsync(student.Id, teacher.Id, ct);
        return deleted ? Result.Success() : Result.Failure(HasLessonPlans);
    }

    private static List<Error> WithEnglishLevelCheck(StudentRequest request, IEnumerable<Error> domainErrors)
    {
        var errors = request.EnglishLevel is null ? [EnglishLevelRequired] : new List<Error>();
        errors.AddRange(domainErrors);
        return errors;
    }

    private async Task<Result<StudentResponse>> ResponseAsync(Student student, CancellationToken ct) =>
        Result<StudentResponse>.Success(StudentResponse.From(student, await LessonPlanCountAsync(student.Id, ct)));

    private async Task<int> LessonPlanCountAsync(Guid studentId, CancellationToken ct)
    {
        var counts = await lessonPlans.CountByStudentsAsync([studentId], ct);
        return counts.GetValueOrDefault(studentId);
    }
}
