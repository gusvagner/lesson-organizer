using LessonOrganizer.Api.Http;
using LessonOrganizer.Application.Students;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Controllers;

[ApiController]
[Route("api/students")]
public sealed class StudentsController(StudentService students) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct) =>
        Ok(await students.ListAsync(search, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ResultHttpMapping.ToOk(await students.GetAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(StudentRequest request, CancellationToken ct) =>
        ResultHttpMapping.ToCreated(await students.CreateAsync(request, ct), nameof(Get), student => new { id = student.Id });

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, StudentRequest request, CancellationToken ct) =>
        ResultHttpMapping.ToOk(await students.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ResultHttpMapping.ToNoContent(await students.DeleteAsync(id, ct));
}
