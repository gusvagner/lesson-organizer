# Prompt 01 — generate the task API (v5: v4 ran part 1; v5 adds the review agent from part 2 on)

You are a senior .NET engineer. Build a small, production-quality RESTful API for task management in the folder `generative-ai-tools/` of this repository. Work only inside that folder.

The work is split into **parts**. Do one part at a time, then **stop and wait** until you are told to continue. Do not start the next part on your own.

**Strict TDD in every part:**

1. Write the tests for the part.
2. Run them and confirm they fail for the expected reason — a missing type or a wrong result, not a typo or a broken setup — and paste that failing output in `generation-log.md`.
3. Implement the minimum to make them pass.
4. Run again and paste the passing output.
5. Refactor with the tests green.

A test that passes before its code exists is a finding: say so in the log instead of hiding it.

**Independent review in every part** — after the part is green and before you stop:

1. Dispatch a fresh reviewer agent (a subagent with no memory of your work). Give it this prompt, the number of the part, and the paths of the part's files and of `generation-log.md`. Tell it to read the code against the prompt, re-run `dotnet build` and `dotnet test` itself, check that the red run in the log is real and failed for the right reason, and report findings graded Critical / Important / Minor, each with file, line and a concrete failure scenario. The reviewer must not edit any file.
2. Paste the reviewer's report in `generation-log.md` as it came.
3. Decide each finding and write the decision next to it: **fixed** (write a failing test first, then the fix, and paste both runs), **declined** (why the code stands), or **deferred** (for a later part, and which one). Do not accept a finding just because the reviewer said it; reviewers can be wrong.
4. Re-run `dotnet build` and `dotnet test` after the fixes, paste the output, then stop.

If you cannot dispatch a subagent, say so in the log and in your report, and stop: the human will run the review.

## Goal

Users manage their own tasks. A task has a title, a description, a status and a due date, and belongs to exactly one user. Assume a basic `User` model already exists: there is no registration or login in this API.

## Requirements

- A **RESTful** API over one resource, `tasks`: plural noun in the path, no verbs in URLs, HTTP methods with their standard meaning (GET reads, POST creates, PUT replaces, DELETE removes), the status codes in the endpoints table, `Location` on create, PUT idempotent.
- Full **CRUD** on tasks: create, read (one task and a paged list), update, delete.
- Each task has a **title**, a **description**, a **status** and a **due date** (`dueDate` in JSON, camelCase like every field).
- Each task belongs to one **user**; the `User` model is assumed to exist.

## Stack and environment (use exactly these; do not query package registries)

- .NET 10 (SDK 10.0.403 is installed), ASP.NET Core Web API with controllers.
- EF Core 10.0.9 with SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`).
- JWT bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9). Mint test tokens with `JsonWebTokenHandler` (`Microsoft.IdentityModel.JsonWebTokens`, already a dependency of JwtBearer); add no other token package.
- Tests: xUnit v3 4.0.1 on Microsoft Testing Platform, `Microsoft.AspNetCore.Mvc.Testing` 10.0.9, `Testcontainers.MsSql` 4.15.0. Docker is running and the image `mcr.microsoft.com/mssql/server:2022-latest` is available locally.
- Nullable reference types on, warnings as errors, central package versions in `Directory.Packages.props`.
- `global.json`, `nuget.config` (nuget.org only), `Directory.Build.props` and `Directory.Packages.props` already exist in `generative-ai-tools/`; extend them, do not replace their intent.

## Architecture

A lean API: one web project and one test project. Dependencies point inward: controller → service → repository. No layer reaches past the next one.

```
generative-ai-tools/
├── TaskManagement.slnx
├── src/TaskManagement.Api/
│   ├── Program.cs                 composition only: services, middleware, endpoints
│   ├── Auth/
│   │   ├── JwtOptions.cs          Issuer, Audience, SigningKey; validated at startup (key ≥ 32 bytes)
│   │   ├── JwtSetup.cs            AddJwtAuthentication(...)
│   │   └── CurrentUser.cs         ICurrentUser from the `sub` claim
│   ├── Tasks/
│   │   ├── TaskItem.cs            entity
│   │   ├── TaskItemStatus.cs      Todo | InProgress | Done (not `TaskStatus`: it collides with System.Threading.Tasks.TaskStatus)
│   │   ├── TaskContracts.cs       CreateTaskRequest, UpdateTaskRequest, TaskResponse, TaskListQuery, PagedResponse<T>
│   │   ├── TaskValidator.cs       hand-written rules → field errors
│   │   ├── TaskService.cs         business rules; depends on ITaskRepository, ICurrentUser, TimeProvider
│   │   ├── ITaskRepository.cs
│   │   └── TasksController.cs     HTTP only: binds, calls the service, maps results to status codes
│   ├── Users/User.cs              entity (no endpoints)
│   ├── Data/
│   │   ├── TaskManagementDbContext.cs
│   │   ├── Configurations/        IEntityTypeConfiguration per entity (lengths, required, FK, indexes)
│   │   ├── EfTaskRepository.cs
│   │   └── Migrations/
│   └── Errors/                    ProblemDetails setup and the exception handler
└── tests/TaskManagement.Tests/
    ├── Unit/                      TaskService and TaskValidator with named fakes (FakeTaskRepository, FixedCurrentUser)
    └── Integration/               ApiFactory, SqlServerFixture (one container per test run), TestTokens, endpoint tests
```

Rules per kind of code:

- **Controller:** no business rules, no EF; reads nothing about the user except through `ICurrentUser`.
- **Service:** returns a small result type (success, validation errors by field, not found) instead of throwing for expected outcomes; the controller maps it (200/201/204, 400 ValidationProblemDetails, 404 ProblemDetails). Every query is scoped to the current user.
- **Repository:** data access only; every method takes the owner id; never loads another user's task.
- **Entities** never leave the API: DTOs in and out.
- **Time** from `TimeProvider` (UTC) so tests can fix it. Configuration through options classes, no magic strings spread around.

## Data model

- `User`: `Id` (Guid), `Email` (unique), `Name`. Table `Users`; no endpoints.
- `TaskItem`: `Id` (Guid), `UserId` (owner, required, FK to `Users`, indexed), `Title` (required, trimmed, 1–200 chars), `Description` (optional, up to 2000 chars, blank → null), `Status` (stored as a string; new tasks start as `Todo`), `DueDate` (`DateOnly`, optional), `CreatedAt`, `UpdatedAt` (UTC `DateTimeOffset`).

## Authentication and authorization

- Every endpoint requires a valid JWT bearer token. Validate issuer, audience, lifetime and signing key from configuration (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey`). No secrets in committed files: development uses user secrets, tests set their own values.
- The current user id comes only from the token's `sub` claim — never from the route, query or body. A token whose `sub` is not a Guid is rejected.
- A user can only see and change their own tasks. Another user's task returns **404**, exactly like a task that does not exist.

## Endpoints

| Method | Path | Success | Errors |
|---|---|---|---|
| GET | `/api/tasks?status=&page=&pageSize=` | 200 `PagedResponse<TaskResponse>`, the caller's tasks sorted by due date (no date last), then creation time | 400 invalid status or paging |
| GET | `/api/tasks/{id}` | 200 `TaskResponse` | 404 |
| POST | `/api/tasks` | 201 + `Location`, status `Todo` | 400 |
| PUT | `/api/tasks/{id}` | 200 (title, description, status, due date) | 400, 404 |
| DELETE | `/api/tasks/{id}` | 204 | 404 |

- Enums as strings in JSON; reject unknown values and numbers.
- Errors as RFC 9457 ProblemDetails; validation errors keyed by camelCase field name; no stack traces or exception messages outside Development.
- Paging: `page` ≥ 1, `pageSize` 1–100 (default 20); the response has `items`, `page`, `pageSize`, `totalCount`.

## Parts

Each part ends with: `dotnet build` (no warnings) and `dotnet test`, both from `generative-ai-tools/`, and an entry in `generation-log.md` with what was built, the failing run (red) and the passing run (green) with the exact commands and their real output (test counts, failures), the reviewer's report with your decision on each finding, and anything you are unsure about. Then stop.

1. **Skeleton.** Solution, both projects, folder layout, `Program.cs` composition, options classes. One smoke test that the app starts. *Done when:* build and test green.
2. **Tasks core.** `TaskItem`, `TaskItemStatus`, contracts, `TaskValidator`, `ITaskRepository`, `TaskService`, and their unit tests with named fakes: validation (missing/long title, long description, blank description → null), default status, trimming, `UpdatedAt` changes on update, not found for another user's task. *Done when:* unit tests green.
3. **Persistence.** `User`, `DbContext`, configurations, `EfTaskRepository`, the initial migration, and integration tests on a real SQL Server: the migration applies on an empty database, length limits and the FK are enforced by the database, queries never return another user's tasks, filter, sorting and paging. *Done when:* integration tests green.
4. **HTTP and auth.** `TasksController`, JWT setup, `ICurrentUser`, ProblemDetails, and integration tests through `WebApplicationFactory` with minted tokens: no token / expired / wrong key / non-Guid `sub` → 401; user B gets 404 on GET, PUT and DELETE of user A's task and never sees it in the list; 400 cases with field errors (including unknown and numeric status, invalid paging); create → 201 with `Location`; full CRUD round trip. *Done when:* all tests green.
5. **Final check.** Re-run everything from a clean build, review your own code against this prompt, and write in `generation-log.md` a short list of anything not done or done differently, with the reason.

## Rules

- Keep it small and readable: no MediatR, AutoMapper, generic repositories or mocking libraries (hand-written fakes only).
- Migrations generated with `dotnet ef` (add a local tool manifest if needed); tests apply them.
- Do not commit; leave the files in the working tree.
- Never claim a command ran or a test passed unless you ran it and saw the output.
