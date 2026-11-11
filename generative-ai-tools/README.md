# GenAI exercise — a task API built by an AI agent and checked at every step

The exercise's GenAI section imagines a RESTful task-management API (CRUD on tasks with a title, a description, a status and a due date, each task tied to a user) and asks for three things: the prompt I would give a GenAI coding tool, the code it produces, and how I validated, corrected and improved that code, including edge cases, authentication and validation.

Instead of describing it, I did it: the prompt below really ran, its output is in this folder, and every step of the checking is recorded.

| The exercise asks for | Where it is |
|---|---|
| The prompt | [prompts/01-generate.md](prompts/01-generate.md), and how it was improved before it ran: [prompt-iterations.md](prompt-iterations.md) |
| The output code | `src/` and `tests/` (sample below); the raw output of each part is a separate commit on the branch |
| How the suggestions were validated | [The workflow](#the-workflow) and my per-part checks: [validation-log.md](validation-log.md) |
| What was corrected, and why | [What the AI got wrong](#what-the-ai-got-wrong-and-how-it-was-caught); the agent's own record: [generation-log.md](generation-log.md) |
| Edge cases, authentication, validation | [How they ended up](#edge-cases-authentication-and-validation) |
| Running it | [RUNNING.md](RUNNING.md) (followed end to end by me) |

## The workflow

The tool is **Claude Code**. I work with it the way I do day to day: a detailed prompt, an agent that builds in small verified steps, independent agents that review, and a person who decides.

1. **The prompt was iterated before it ran** — five versions, each change raised by me with its reason ([prompt-iterations.md](prompt-iterations.md)): architecture and parts (v2), strict TDD (v3), REST and CRUD made explicit (v4), an independent review agent in every part (v5).
2. **One fresh agent built it in five parts** — skeleton, tasks core, persistence, HTTP and auth, final check. It saw only the prompt and this folder, which was isolated from the main project (own `global.json`, `nuget.config`, build and style settings) so that a failure would be the AI's, not the environment's.
3. **Strict TDD in every part** — tests written first, run red, the failing output pasted in the log, then green. When a test passed before its code existed, the agent had to say so instead of hiding it.
4. **An independent reviewer agent after every part** — a fresh subagent re-ran build and tests, checked that the red run was real, and graded findings. Its report went into the log as it came. The building agent then decided each finding — fixed (test first), declined (with the reason) or deferred (and to which part) — and was told not to accept a finding just because a reviewer said it.
5. **My validation after every part, before the next one started** — I re-ran build, format and tests myself instead of trusting the report, checked the red runs, read the code against the prompt, judged every reviewer finding and every decision, and committed the raw output. After the last part I followed `RUNNING.md` by hand against a throwaway database and ran the API.

| Part | What | Tests after the part |
|---|---|---|
| 1 | Skeleton, JWT options validated at startup (+ its first agent review) | 15 |
| 2 | Tasks core: entity, validator, service | 59 |
| 3 | Persistence: EF Core, migration, SQL Server tests | 87 |
| 4 | HTTP and JWT authentication | 162 |
| 5 | Clean rebuild, self-review, run instructions, review of the whole project | **168** |

About 3,100 lines of C# (migrations excluded), 6 commits. `dotnet build` with 0 warnings, `dotnet format` clean, 168 tests green against a real SQL Server.

## The prompt, in short

The full text is [prompts/01-generate.md](prompts/01-generate.md). What makes it work:

- **Exact stack and versions**, and "do not query package registries": the agent cannot drift to other versions or tools.
- **Requirements stated as rules**, not hopes: a plural resource, no verbs in URLs, standard status codes, `Location` on create, idempotent PUT, full CRUD, `dueDate` in camelCase.
- **Architecture with a reason per rule**: thin controller, a service that returns a result instead of throwing, a repository that always takes the owner, DTOs only, `TimeProvider` for testable time, and a warning about a likely mistake (`TaskStatus` collides with `System.Threading.Tasks.TaskStatus`).
- **Security stated explicitly**: the user id only from the token's `sub`, another user's task answers 404 exactly like a missing one, no secrets in committed files.
- **The tests it must have**, named one by one (401 cases, cross-user 404s, 400 cases with field errors, the CRUD round trip).
- **A definition of done that cannot be faked**: build without warnings, tests green, red and green output pasted, "never claim a command ran unless you ran it".
- **Small steps with a stop after each**, so a person checks before the next one builds on it.

## The output — a sample

The generated API: `src/TaskManagement.Api` (controllers, a service with the rules, EF Core) and `tests/TaskManagement.Tests` (unit tests with hand-written fakes, integration tests through `WebApplicationFactory` on a real SQL Server started by Testcontainers).

Every query is scoped to the owner, so another user's task is simply not found:

```csharp
// src/TaskManagement.Api/Data/EfTaskRepository.cs
public Task<TaskItem?> FindAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
    OwnedBy(ownerId).SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);

private IQueryable<TaskItem> OwnedBy(Guid ownerId) => dbContext.Tasks.Where(task => task.UserId == ownerId);
```

A token is accepted only when its subject names a real user (this closed a 500 found by a reviewer):

```csharp
// src/TaskManagement.Api/Auth/SubjectValidatingJwtEvents.cs
public override async Task TokenValidated(TokenValidatedContext context)
{
    var subject = CurrentUser.ReadSubject(context.Principal);
    if (!Guid.TryParse(subject, out var userId))
    {
        context.Fail($"The token's 'sub' claim must be a Guid; got '{subject}'.");
        return;
    }

    if (!await users.ExistsAsync(userId, context.HttpContext.RequestAborted))
    {
        context.Fail($"The token's 'sub' claim names user {userId}, who does not exist.");
    }
}
```

The controller only maps the service's result to HTTP; a missing task and another user's task get the same 404:

```csharp
// src/TaskManagement.Api/Tasks/TasksController.cs
private IActionResult ToActionResult<T>(TaskResult<T> result, Func<T, IActionResult> onSuccess) => result switch
{
    TaskResult<T>.Success success => onSuccess(success.Value),
    TaskResult<T>.ValidationFailed invalid => FieldValidationProblem(invalid.Errors),
    TaskResult<T>.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: TaskNotFoundTitle),
    _ => throw new InvalidOperationException($"Unhandled task result {result.GetType().Name}; expected Success, ValidationFailed or NotFound."),
};
```

## What the AI got wrong, and how it was caught

Each of these was in code the agent wrote first and believed finished; each was caught by a reviewer agent, a test or the build, and fixed with a test that failed first.

| What went wrong | Caught by | Fix |
|---|---|---|
| JSON `"status":"Todo, InProgress"` was accepted and stored as `InProgress` — the standard enum converter parses flags syntax even for a non-flags enum | The part 4 reviewer marked the case *not verified*; the agent tested it instead of letting it pass | A name-only converter shared with the query binder; numbers and lists → 400 |
| A token with a valid Guid `sub` but no user row reached the foreign key → 500 | Part 2 reviewer (Important), carried to part 4 by my validation | Check at token validation → 401; tests on GET and POST |
| A task deleted between "find" and "update" threw → 500 | Part 3 reviewer | Repository returns false, service maps it to not found → 404, tested with two database contexts and over HTTP |
| A huge `page` overflowed the row offset → a 500 on SQL Server | Part 2 reviewer (Important) | Validator rejects any page whose offset does not fit, with a test at the exact limit |
| A signing key of 32 spaces passed validation | Part 1 reviewer (I had missed it) | Whitespace-only keys rejected |
| The 401 response header echoed exception text (`IncludeErrorDetails`) | Part 4 reviewer | Turned off in every environment |
| The database only enforced the title's maximum length | Part 3 reviewer | Check constraint `CK_Tasks_Title_NotBlank`, tested by SQL error number and constraint name |
| Problem responses came out as `application/json`; validation errors came out as `Page` instead of `page` | The agent's own red runs in part 4 | Removed a `[Produces]` attribute; the controller builds the problem through the camelCase formatter |
| A vulnerable transitive package (`System.Security.Cryptography.Pkcs` 9.0.4) arrived with EF Core | The build (NU1902 as an error, warnings as errors) | Pinned centrally to 10.0.13 |
| The dev-token script would lose its executable bit when committed from Windows | Part 5 reviewer (Important) | `RUNNING.md` runs it with `bash` |

**Where an AI reviewer was wrong, and the claim was checked instead of accepted:**

- Part 1: the reviewer said a compiler error was missing from the red-run excerpt. The agent re-read the saved raw output, showed the error never existed (the compiler does not repeat it after a failed `using`), and admitted the part that was right — it had trimmed duplicate lines without saying so.
- Part 2: the reviewer said the default-status test could not tell `Todo` from "never set". True, and no unit test can: declined, covered by the string-status tests in parts 3 and 4.
- Part 5: both the agent and the reviewer expected the wrong error key for truncated JSON; the agent fixed its expectation, not the code.

**Mistakes that were mine, not the AI's:** my folder isolation first missed an `.editorconfig`, so the parent project's style rules leaked in (found by the agent, fixed by me); and when I followed the run instructions I left an API process running, which made my next start fail on a busy port.

## Edge cases, authentication and validation

**Authentication** — JWT bearer: HS256 only, signed tokens with an expiry required, issuer, audience and key validated, 30 s clock skew, no error details in the 401. The user id comes only from `sub`; a `sub` that is not a Guid or names no user is rejected. A fallback policy requires a token on every path, so a new endpoint cannot be anonymous by accident. The signing key is validated at startup (≥ 32 bytes, not blank) and never written to a committed file.

**Authorization** — every repository query takes the owner; user B gets 404 on GET, PUT and DELETE of user A's task — the same response as a missing task — and never sees it in the list. The owner of a new task comes from the token; the request has no user field.

**Validation** — title required, trimmed, 1–200 characters; description up to 2000, blank → null; status only `Todo`, `InProgress`, `Done` (unknown names, numbers and comma lists → 400); `dueDate` a date only; `page` ≥ 1 and an offset that fits, `pageSize` 1–100. Errors are RFC 9457 ProblemDetails with camelCase field keys; no stack traces or exception messages outside Development. The database repeats the important rules (lengths, required columns, the foreign key, a non-blank title).

**Edge cases covered by tests** — expired, not-yet-valid, wrong-key, unsigned, wrong-issuer and wrong-audience tokens; a deleted user's token; the find-then-update race; paging at the exact overflow limit; empty, `[]` and truncated JSON bodies; signing keys counted in UTF-8 bytes; sorting with no due date last and a stable order.

**Accepted as they are** (decided in [validation-log.md](validation-log.md)): unknown JSON properties are ignored; an identical PUT still moves `updatedAt`; the database accepts a title of tabs or line breaks (the validator does not); every test needs Docker; one user lookup per request; the API serves plain HTTP and expects TLS at a proxy in front of it.

## What this shows

The generated code is good — but the value was in the checking. Seven of the ten fixes above came from reviewer agents, one from treating a reviewer's "not verified" as an open question, one from the agent's own red runs, and one from the build. Reviewers were wrong too, and their claims were tested against evidence before anyone acted on them. And a person validated every part before the next one was built on top of it.
