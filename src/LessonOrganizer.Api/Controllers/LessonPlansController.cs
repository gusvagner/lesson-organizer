using LessonOrganizer.Api.Http;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Controllers;

[ApiController]
[Route("api/lesson-plans")]
public sealed class LessonPlansController(LessonPlanService lessonPlans) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? studentId, [FromQuery] LessonPlanStatus? status, CancellationToken ct) =>
        ResultHttpMapping.ToOk(await lessonPlans.ListAsync(new LessonPlanFilter(studentId, status), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ResultHttpMapping.ToOk(await lessonPlans.GetAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(LessonPlanRequest request, CancellationToken ct) =>
        ResultHttpMapping.ToCreated(await lessonPlans.CreateAsync(request, ct), nameof(Get), plan => new { id = plan.Id });

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, LessonPlanRequest request, CancellationToken ct) =>
        ResultHttpMapping.ToOk(await lessonPlans.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeLessonPlanStatusRequest request, CancellationToken ct) =>
        ResultHttpMapping.ToOk(await lessonPlans.ChangeStatusAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ResultHttpMapping.ToNoContent(await lessonPlans.DeleteAsync(id, ct));
}
