using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Errors;

namespace TaskManagement.Api.Tasks;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(TaskService taskService) : ControllerBase
{
    private const string TaskNotFoundTitle = "Task not found";

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TaskListQuery query, CancellationToken cancellationToken) =>
        ToActionResult(await taskService.ListAsync(query, cancellationToken), Ok);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await taskService.GetAsync(id, cancellationToken), Ok);

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken) =>
        ToActionResult(
            await taskService.CreateAsync(request, cancellationToken),
            created => CreatedAtAction(nameof(Get), new { id = created.Id }, created));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await taskService.UpdateAsync(id, request, cancellationToken), Ok);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await taskService.DeleteAsync(id, cancellationToken), _ => NoContent());

    private IActionResult ToActionResult<T>(TaskResult<T> result, Func<T, IActionResult> onSuccess) => result switch
    {
        TaskResult<T>.Success success => onSuccess(success.Value),
        TaskResult<T>.ValidationFailed invalid => FieldValidationProblem(invalid.Errors),
        TaskResult<T>.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: TaskNotFoundTitle),
        _ => throw new InvalidOperationException($"Unhandled task result {result.GetType().Name}; expected Success, ValidationFailed or NotFound."),
    };

    private IActionResult FieldValidationProblem(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(field, message);
            }
        }

        return ValidationProblemResponse.Create(ControllerContext);
    }
}
