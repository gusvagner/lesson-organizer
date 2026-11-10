using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.LessonPlans;

public sealed class LessonPlanService(ILessonPlanRepository lessonPlans, IStudentRepository students, ICurrentTeacher teacher)
{
    private static readonly Error NotFound = Error.NotFound("Lesson plan not found.");
    private static readonly Error StudentNotFound = Error.Validation("studentId", "Student not found.");
    private static readonly Error LessonDateRequired = Error.Validation("lessonDate", "Lesson date is required.");
    private static readonly Error StatusRequired = Error.Validation("status", "Status is required.");

    public async Task<IReadOnlyList<LessonPlanResponse>> ListAsync(LessonPlanFilter filter, CancellationToken ct)
    {
        var plans = await lessonPlans.ListByTeacherAsync(teacher.Id, filter, ct);
        var ownStudents = await students.ListByTeacherAsync(teacher.Id, null, ct);
        var names = ownStudents.ToDictionary(student => student.Id, student => student.Name);
        return plans.Select(plan => LessonPlanResponse.From(plan, names[plan.StudentId])).ToList();
    }

    public async Task<Result<LessonPlanResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var plan = await lessonPlans.GetOwnedAsync(id, teacher.Id, ct);
        return plan is null ? Result<LessonPlanResponse>.Failure(NotFound) : await ResponseAsync(plan, ct);
    }

    public async Task<Result<LessonPlanResponse>> CreateAsync(LessonPlanRequest request, CancellationToken ct)
    {
        var (student, errors) = await ValidateAsync(request, ct);
        if (errors.Count > 0)
        {
            return Result<LessonPlanResponse>.Failure(errors);
        }

        var plan = LessonPlan.Create(student!.Id, request.Title, request.Description, request.LessonDate!.Value).Value;
        await lessonPlans.AddAsync(plan, ct);
        return Result<LessonPlanResponse>.Success(LessonPlanResponse.From(plan, student.Name));
    }

    public async Task<Result<LessonPlanResponse>> UpdateAsync(Guid id, LessonPlanRequest request, CancellationToken ct)
    {
        var plan = await lessonPlans.GetOwnedAsync(id, teacher.Id, ct);
        if (plan is null)
        {
            return Result<LessonPlanResponse>.Failure(NotFound);
        }

        var (student, errors) = await ValidateAsync(request, ct);
        if (errors.Count > 0)
        {
            return Result<LessonPlanResponse>.Failure(errors);
        }

        plan.Update(student!.Id, request.Title, request.Description, request.LessonDate!.Value);
        await lessonPlans.UpdateAsync(plan, ct);
        return Result<LessonPlanResponse>.Success(LessonPlanResponse.From(plan, student.Name));
    }

    public async Task<Result<LessonPlanResponse>> ChangeStatusAsync(Guid id, ChangeLessonPlanStatusRequest request, CancellationToken ct)
    {
        var plan = await lessonPlans.GetOwnedAsync(id, teacher.Id, ct);
        if (plan is null)
        {
            return Result<LessonPlanResponse>.Failure(NotFound);
        }

        var changed = request.Status is null ? Result.Failure(StatusRequired) : plan.ChangeStatus(request.Status.Value);
        if (!changed.IsSuccess)
        {
            return Result<LessonPlanResponse>.Failure(changed.Errors);
        }

        await lessonPlans.UpdateAsync(plan, ct);
        return await ResponseAsync(plan, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var plan = await lessonPlans.GetOwnedAsync(id, teacher.Id, ct);
        if (plan is null)
        {
            return Result.Failure(NotFound);
        }

        await lessonPlans.DeleteAsync(plan.Id, ct);
        return Result.Success();
    }

    private async Task<(Student? Student, List<Error> Errors)> ValidateAsync(LessonPlanRequest request, CancellationToken ct)
    {
        var errors = new List<Error>();
        var student = request.StudentId is { } studentId ? await students.GetOwnedAsync(studentId, teacher.Id, ct) : null;
        if (student is null)
        {
            errors.Add(StudentNotFound);
        }

        if (request.LessonDate is null)
        {
            errors.Add(LessonDateRequired);
        }

        var draft = LessonPlan.Create(Guid.Empty, request.Title, request.Description, request.LessonDate ?? default);
        errors.AddRange(draft.Errors);
        return (student, errors);
    }

    private async Task<Result<LessonPlanResponse>> ResponseAsync(LessonPlan plan, CancellationToken ct)
    {
        var student = await students.GetOwnedAsync(plan.StudentId, teacher.Id, ct);
        return Result<LessonPlanResponse>.Success(LessonPlanResponse.From(plan, student!.Name));
    }
}
