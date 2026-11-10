using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Domain.LessonPlans;

public sealed class LessonPlan
{
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 5000;

    private LessonPlan()
    {
        Title = "";
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public DateOnly LessonDate { get; private set; }

    public LessonPlanStatus Status { get; private set; }

    public static Result<LessonPlan> Create(Guid studentId, string? title, string? description, DateOnly lessonDate)
    {
        var plan = new LessonPlan { Id = Guid.CreateVersion7(), Status = LessonPlanStatus.ToPrepare };
        var result = plan.Update(studentId, title, description, lessonDate);
        return result.IsSuccess ? Result<LessonPlan>.Success(plan) : Result<LessonPlan>.Failure(result.Errors);
    }

    public Result Update(Guid studentId, string? title, string? description, DateOnly lessonDate)
    {
        var errors = new List<Error>();
        var validTitle = TextField.Required(title, "title", "Title", TitleMaxLength, errors);
        var validDescription = TextField.Optional(description, "description", "Description", DescriptionMaxLength, errors);
        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        StudentId = studentId;
        Title = validTitle!;
        Description = validDescription;
        LessonDate = lessonDate;
        return Result.Success();
    }

    public Result ChangeStatus(LessonPlanStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            return Result.Failure(Error.Validation(
                "status",
                $"Status must be one of: {string.Join(", ", Enum.GetNames<LessonPlanStatus>())}."));
        }

        Status = status;
        return Result.Success();
    }
}
