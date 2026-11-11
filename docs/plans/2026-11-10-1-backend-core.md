# Backend Core Implementation Plan (stages 0–4)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A .NET 10 solution with the Domain, Application and Infrastructure layers working and tested: entities with their rules, the student and lesson plan use cases, and the SQL Server data layer with EF Core. No HTTP yet.

**Architecture:** Clean Architecture with four projects; this plan fills Domain, Application and Infrastructure and creates an empty Api. Domain returns `Result`/`Result<T>` instead of throwing; Application services enforce ownership through `ICurrentTeacher` and repository interfaces; Infrastructure implements the repositories with EF Core over SQL Server, tested against a real SQL Server started by Testcontainers.

**Tech Stack:** .NET SDK 10.0.403, `net10.0`, C# 14, xUnit v3, EF Core 10 (SQL Server provider), ASP.NET Core Identity EF Core stores (user tables only in this plan), Testcontainers for SQL Server, Docker.

**Spec:** [`docs/plan.md`](../plan.md) (§2 ambiguities, §3 data model, §5 contracts, §6 acceptance criteria), [`docs/architecture.md`](../architecture.md), [`docs/stack.md`](../stack.md), [ADR 0003](../decisions/0003-sql-server-ef-core-docker.md). Exercise statement: `docs/Net - BLA - Technical Interview Exercise - V6 (1).pdf` (local only).

**Plan series** (listed in [`plan.md` §7](../plan.md#7-implementation-stages), each written just before it runs): 1 Backend core (this) · 2 Backend API (stages 5–8) · 3 Frontend (stages 9–11) · 4 Delivery (stage 12) · 5 Security review (stage 13).

## Global Constraints

- SDK `10.0.403` in `global.json` with `rollForward: latestFeature`; `TargetFramework` `net10.0` only in `Directory.Build.props`.
- `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended, `EnforceCodeStyleInBuild` true.
- Packages only from `docs/stack.md`: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` (Task 1); `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Testcontainers.MsSql` and the `dotnet-ef` tool (Task 7). EF Core, Identity and `dotnet-ef` on 10.0.x. Versions only in `Directory.Packages.props` (tool: `.config/dotnet-tools.json`).
- **Never query a package repository** (private feeds, nuget.org) or image registry to find versions. Use the local NuGet cache or ask the human. Pulling the SQL Server image is done or approved by the human.
- Domain and Application reference no ASP.NET Core, Identity or EF Core. Domain references nothing; Application references Domain only.
- No MediatR, AutoMapper, FluentValidation, generic repository, mocking library. Tests use named fakes; data-layer tests use the real SQL Server.
- No connection string or password in committed files.
- No explanatory comments in code; `<summary>` only on a class whose purpose the name does not make clear.
- Methods 4–20 lines, files under 500 lines, max 2 levels of indentation, early returns.
- Exception messages include the offending value and the expected shape.
- Test names `Method_Scenario_ExpectedResult`. All async repository/service methods end in `Async` and take a `CancellationToken`.
- Everything in English. Conventional Commits. Strict TDD: red commit (`test:`) then green commit (`feat:`) when practical.
- Error `Field` values are the camelCase JSON names: `name`, `email`, `englishLevel`, `notes`, `studentId`, `title`, `description`, `lessonDate`, `status`.
- After each task, append the failing and passing test runs (command + summary line) to `docs/ai/AI_LOG.md`.

## Review Focus

1. **Numeric enum values** (`EnglishLevel` 99, `LessonPlanStatus` 7): must be a validation error, not stored. Pinned in Task 3 and Task 4.
2. **Invalid update leaves the entity unchanged:** a failed `Update` must not half-apply the valid fields. Pinned in Task 3 and Task 4.
3. **Email in display-name form** (`Ana <ana@example.com>`): `MailAddress` accepts it; it must be rejected. Pinned in Task 2.
4. **Wildcards in the search text** (`50%`, `a_b`, `[x]`): SQL `LIKE` treats them as patterns; they must match literally. Pinned in Task 8.
5. **Filter by another teacher's student** returns an empty list, not an error (plan A4). Pinned in Task 6.

---

### Task 1: Scaffold the solution (stage 0)

**Files:**
- Create: `global.json`, `Directory.Build.props`, `tests/Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `LessonOrganizer.slnx`, `README.md`
- Create projects: `src/LessonOrganizer.Domain`, `src/LessonOrganizer.Application`, `src/LessonOrganizer.Infrastructure`, `src/LessonOrganizer.Api`; `tests/LessonOrganizer.Domain.Tests`, `tests/LessonOrganizer.Application.Tests`, `tests/LessonOrganizer.Infrastructure.Tests`, `tests/LessonOrganizer.Api.Tests`, `tests/LessonOrganizer.Api.IntegrationTests`
- Modify: `CLAUDE.md` (Commands section)

**Interfaces:**
- Produces: the project graph `Api → Infrastructure → Application → Domain`, `Api → Application`; each test project references its layer (`Api.Tests` and `Api.IntegrationTests` reference `Api`).

- [ ] **Step 1: Human checkpoint — package source.** Ask the human where packages come from (do not query any feed). If they choose nuget.org only, add a repo `nuget.config` with `<clear />` and nuget.org.
- [ ] **Step 2: Pick test package versions without querying a feed.** List folders under `%USERPROFILE%\.nuget\packages\xunit.v3`, `xunit.runner.visualstudio`, `microsoft.net.test.sdk`, `coverlet.collector`; take the highest stable version of each. If one is missing, ask the human for the version.
- [ ] **Step 3: Write `global.json`, `Directory.Build.props`, `Directory.Packages.props`** with the values in `docs/stack.md` and the versions from Step 2. `tests/Directory.Build.props` imports the root one (`<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`) and adds `IsPackable=false`, `IsTestProject=true` and the four test packages, so test `.csproj` files list no packages.
- [ ] **Step 4: Create the projects.** `dotnet new sln --format slnx -n LessonOrganizer`; `classlib` for Domain, Application, Infrastructure; `web` (empty) for Api; `xunit3` template if installed, otherwise `classlib` with `IsTestProject=true`, for the five test projects. Remove template `Class1.cs`/`UnitTest1.cs`. Remove any `TargetFramework` or package version from the `.csproj` files. Add references as in Interfaces.
- [ ] **Step 5: `.editorconfig`** with the .NET defaults: 4-space indent, file-scoped namespaces, `var` when type is apparent, `_camelCase` private fields, `Async` suffix rule as warning. Exclude `**/Migrations/*.cs` from style rules (generated code).
- [ ] **Step 6: README skeleton** with the 11 headings from `plan.md` §7 "README contents", each with "To be written in stage N".
- [ ] **Step 7: Verify.**
  Run: `dotnet build` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
  Run: `dotnet test` → exits 0 (no tests yet).
- [ ] **Step 8: Fill `CLAUDE.md` Commands** with the backend rows of `docs/stack.md` "Commands" that already work (build, test, run, format); the rest are added when their stage lands.
- [ ] **Step 9: Commit** `chore: scaffold solution, projects and build settings`.

---

### Task 2: Domain common types (stage 1)

**Files:**
- Create: `src/LessonOrganizer.Domain/Common/ErrorKind.cs`, `Error.cs`, `Result.cs`, `ResultOfT.cs`, `TextField.cs`, `EmailFormat.cs`
- Test: `tests/LessonOrganizer.Domain.Tests/Common/ResultTests.cs`, `TextFieldTests.cs`, `EmailFormatTests.cs`, `LayerDependencyTests.cs`

**Interfaces:**
- Produces:
  - `enum ErrorKind { Validation, NotFound, Conflict }`
  - `sealed record Error(ErrorKind Kind, string Message, string? Field = null)` with `static Error Validation(string field, string message)`, `static Error NotFound(string message)`, `static Error Conflict(string message)`
  - `sealed class Result` — `bool IsSuccess`, `IReadOnlyList<Error> Errors`, `static Result Success()`, `static Result Failure(IEnumerable<Error> errors)`, `static Result Failure(Error error)`
  - `sealed class Result<T>` — same members plus `T Value` (throws `InvalidOperationException` on failure, message lists the error messages), `static Result<T> Success(T value)`
  - `static class TextField` — `string? Required(string? value, string field, string label, int maxLength, ICollection<Error> errors)` returns the trimmed value or `null` after adding an error; `string? Optional(string? value, string field, string label, int maxLength, ICollection<Error> errors)` returns trimmed value, `null` for empty/whitespace, adds an error when too long
  - `static class EmailFormat` — `bool IsValid(string email)`
- Exact messages: `"{label} is required."`, `"{label} must be at most {maxLength} characters."`, `"Email is not a valid email address."`

- [ ] **Step 1: Write the failing tests.**

```csharp
[Fact]
public void Required_ValueWithSpacesAround150Chars_ReturnsTrimmedValue()
{
    var errors = new List<Error>();
    var value = TextField.Required("  " + new string('a', 150) + "  ", "name", "Name", 150, errors);
    Assert.Equal(new string('a', 150), value);
    Assert.Empty(errors);
}

[Fact]
public void Required_151CharsAfterTrim_AddsMaxLengthError()
{
    var errors = new List<Error>();
    TextField.Required(new string('a', 151), "name", "Name", 150, errors);
    Assert.Equal(Error.Validation("name", "Name must be at most 150 characters."), Assert.Single(errors));
}

[Theory]
[InlineData("Ana <ana@example.com>")]
[InlineData("ana")]
[InlineData("ana@")]
public void IsValid_NotPlainAddress_ReturnsFalse(string email) => Assert.False(EmailFormat.IsValid(email));
```

  Also: `Required_WhitespaceOnly_AddsRequiredError` (`" \t\n "` → `"Name is required."`), `Required_Null_AddsRequiredError`, `Optional_Whitespace_ReturnsNull`, `Optional_TooLong_AddsMaxLengthError`, `IsValid_PlainAddress_ReturnsTrue` (`"ana@example.com"`), `Value_OnFailure_ThrowsWithErrorMessages` (message contains `"Name is required."`), `Success_HasNoErrors`.
  `LayerDependencyTests.DomainAssembly_ReferencesNoProjectOrFramework`: `typeof(Result).Assembly.GetReferencedAssemblies()` has no name starting with `LessonOrganizer.`, `Microsoft.AspNetCore` or `Microsoft.EntityFrameworkCore`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Domain.Tests` → build fails: `The type or namespace name 'TextField' could not be found`. Commit `test: domain result, text field and email rules`.
- [ ] **Step 3: Implement the types in Interfaces.** `EmailFormat.IsValid`: `MailAddress.TryCreate(email, out var parsed) && parsed.Address == email` (plan A6).
- [ ] **Step 4: Run to verify they pass.** `dotnet test tests/LessonOrganizer.Domain.Tests` → all passed.
- [ ] **Step 5: Commit** `feat: domain result, text field and email rules`.

---

### Task 3: `Student` entity (stage 1)

**Files:**
- Create: `src/LessonOrganizer.Domain/Students/Student.cs`, `src/LessonOrganizer.Domain/Students/EnglishLevel.cs`
- Test: `tests/LessonOrganizer.Domain.Tests/Students/StudentTests.cs`

**Interfaces:**
- Consumes: Task 2 types.
- Produces:
  - `enum EnglishLevel { Beginner, Elementary, Intermediate, UpperIntermediate, Advanced }`
  - `Student` — `Guid Id`, `Guid TeacherId`, `string Name`, `string? Email`, `EnglishLevel EnglishLevel`, `string? Notes` (private setters); `static Result<Student> Create(Guid teacherId, string? name, string? email, EnglishLevel englishLevel, string? notes)`; `Result Update(string? name, string? email, EnglishLevel englishLevel, string? notes)`; a private parameterless constructor for EF Core.
- Limits: name 150 (required), email 254 (optional, valid when present), notes 2000 (optional).
- Ids from `Guid.CreateVersion7()`. Exact enum message: `"English level must be one of: Beginner, Elementary, Intermediate, UpperIntermediate, Advanced."` (field `englishLevel`).
- `Create` and `Update` collect **all** errors, and `Update` changes nothing unless every field is valid.

- [ ] **Step 1: Write the failing tests.**

```csharp
[Fact]
public void Update_InvalidEmail_LeavesStudentUnchanged()
{
    var student = Student.Create(TeacherId, "Ana", null, EnglishLevel.Intermediate, null).Value;
    var result = student.Update("Ana Maria", "not-an-email", EnglishLevel.Advanced, "notes");
    Assert.False(result.IsSuccess);
    Assert.Equal("Ana", student.Name);
    Assert.Equal(EnglishLevel.Intermediate, student.EnglishLevel);
}

[Fact]
public void Create_NumericLevelNotDefined_ReturnsEnglishLevelError()
{
    var result = Student.Create(TeacherId, "Ana", null, (EnglishLevel)99, null);
    Assert.Equal("englishLevel", Assert.Single(result.Errors).Field);
}
```

  Also: `Create_ValidData_TrimsAndStoresNullForEmptyOptionals` (`" Ana "`, `"  "`, notes `""` → `"Ana"`, `null`, `null`), `Create_NameMissingAndEmailInvalid_ReturnsBothErrors`, `Create_NotesOver2000_ReturnsNotesError`, `Create_EmailOver254_ReturnsEmailError`, `Create_GeneratesVersion7Id` (`student.Id.Version == 7`), `Update_ValidData_ChangesAllFields`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Domain.Tests` → build fails, `Student` not found. Commit `test: student invariants`.
- [ ] **Step 3: Implement `EnglishLevel` and `Student`** per Interfaces; enum check with `Enum.IsDefined`.
- [ ] **Step 4: Run to verify they pass.** Same command → all passed.
- [ ] **Step 5: Commit** `feat: student entity`.

---

### Task 4: `LessonPlan` entity (stage 1)

**Files:**
- Create: `src/LessonOrganizer.Domain/LessonPlans/LessonPlan.cs`, `LessonPlanStatus.cs`
- Test: `tests/LessonOrganizer.Domain.Tests/LessonPlans/LessonPlanTests.cs`

**Interfaces:**
- Produces:
  - `enum LessonPlanStatus { ToPrepare, Ready, Delivered }`
  - `LessonPlan` — `Guid Id`, `Guid StudentId`, `string Title`, `string? Description`, `DateOnly LessonDate`, `LessonPlanStatus Status` (private setters); `static Result<LessonPlan> Create(Guid studentId, string? title, string? description, DateOnly lessonDate)`; `Result Update(Guid studentId, string? title, string? description, DateOnly lessonDate)` (status untouched); `Result ChangeStatus(LessonPlanStatus status)`; a private parameterless constructor for EF Core.
- Limits: title 150 required, description 5000 optional. Exact status message: `"Status must be one of: ToPrepare, Ready, Delivered."` (field `status`).

- [ ] **Step 1: Write the failing tests.** `Create_ValidData_StartsAsToPrepare`, `Create_PastDate_IsAccepted` (`DateOnly.FromDateTime(DateTime.Today).AddDays(-30)`), `Create_TitleWhitespaceOnly_ReturnsTitleRequired`, `Create_DescriptionOver5000_ReturnsDescriptionError`, `ChangeStatus_DeliveredBackToToPrepare_Succeeds`, `ChangeStatus_UndefinedValue_ReturnsStatusErrorAndKeepsStatus` (`(LessonPlanStatus)7`), `Update_TitleTooLong_LeavesPlanUnchanged`, `Update_ValidData_KeepsStatus` (plan in `Ready` stays `Ready`).
- [ ] **Step 2: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Domain.Tests` → build fails, `LessonPlan` not found. Commit `test: lesson plan invariants`.
- [ ] **Step 3: Implement** per Interfaces.
- [ ] **Step 4: Run to verify they pass.** Same command → all passed.
- [ ] **Step 5: Commit** `feat: lesson plan entity`.

---

### Task 5: Application ports, fakes and `StudentService` (stage 2)

**Files:**
- Create in `src/LessonOrganizer.Application/`: `Abstractions/ICurrentTeacher.cs`, `IStudentRepository.cs`, `ILessonPlanRepository.cs`; `Students/StudentService.cs`, `StudentRequest.cs`, `StudentResponse.cs`; `LessonPlans/LessonPlanFilter.cs`
- Create in `tests/LessonOrganizer.Application.Tests/`: `Fakes/InMemoryData.cs`, `InMemoryStudentRepository.cs`, `InMemoryLessonPlanRepository.cs`, `FixedCurrentTeacher.cs`; `Students/StudentServiceTests.cs`; `LayerDependencyTests.cs`

**Interfaces:**
- Consumes: Tasks 2–4.
- Produces:
  - `interface ICurrentTeacher { Guid Id { get; } }`
  - `interface IStudentRepository` — `Task AddAsync(Student student, CancellationToken ct)`; `Task<IReadOnlyList<Student>> ListByTeacherAsync(Guid teacherId, string? search, CancellationToken ct)` (sorted by name, then id; case-insensitive search, wildcards literal; blank search = all); `Task<Student?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct)`; `Task UpdateAsync(Student student, CancellationToken ct)`; `Task<bool> TryDeleteAsync(Guid id, Guid teacherId, CancellationToken ct)` (false when it has lesson plans)
  - `interface ILessonPlanRepository` — `Task AddAsync(LessonPlan plan, CancellationToken ct)`; `Task<IReadOnlyList<LessonPlan>> ListByTeacherAsync(Guid teacherId, LessonPlanFilter filter, CancellationToken ct)` (sorted by date, title, id); `Task<LessonPlan?> GetOwnedAsync(Guid id, Guid teacherId, CancellationToken ct)`; `Task<IReadOnlyDictionary<Guid, int>> CountByStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct)`; `Task UpdateAsync(LessonPlan plan, CancellationToken ct)`; `Task DeleteAsync(Guid id, CancellationToken ct)`
  - `sealed record LessonPlanFilter(Guid? StudentId, LessonPlanStatus? Status)`
  - `sealed record StudentRequest(string? Name, string? Email, EnglishLevel? EnglishLevel, string? Notes)`
  - `sealed record StudentResponse(Guid Id, string Name, string? Email, EnglishLevel EnglishLevel, string? Notes, int LessonPlanCount)` with `static StudentResponse From(Student student, int lessonPlanCount)`
  - `sealed class StudentService(IStudentRepository students, ILessonPlanRepository lessonPlans, ICurrentTeacher teacher)` — `ListAsync(string? search, ct) → Task<IReadOnlyList<StudentResponse>>`; `GetAsync(Guid id, ct) → Task<Result<StudentResponse>>`; `CreateAsync(StudentRequest request, ct) → Task<Result<StudentResponse>>`; `UpdateAsync(Guid id, StudentRequest request, ct) → Task<Result<StudentResponse>>`; `DeleteAsync(Guid id, ct) → Task<Result>`
  - Fakes: `InMemoryData` holds `List<Student>`, `List<LessonPlan>`; the two repositories share one instance and implement the same sorting, search and integrity rules as the interfaces state (search with `StringComparison.OrdinalIgnoreCase`). `FixedCurrentTeacher(Guid id)`.
- Exact messages: `"English level is required."` (field `englishLevel`, when the request value is null); `"Student not found."` (NotFound); `"This student has lesson plans. Delete them first or move them to another student."` (Conflict).

- [ ] **Step 1: Write the failing tests** (two teachers, `TeacherA` and `TeacherB`, each with a `FixedCurrentTeacher`).

```csharp
[Fact]
public async Task Delete_StudentHasLessonPlans_ReturnsConflictAndKeepsStudent()
{
    var ana = await SeedStudentAsync(TeacherA, "Ana");
    await SeedLessonPlanAsync(ana.Id, "Present perfect");
    var result = await ServiceFor(TeacherA).DeleteAsync(ana.Id, CancellationToken.None);
    Assert.Equal(ErrorKind.Conflict, Assert.Single(result.Errors).Kind);
    Assert.NotNull(await _students.GetOwnedAsync(ana.Id, TeacherA, CancellationToken.None));
}

[Fact]
public async Task Get_StudentOfAnotherTeacher_ReturnsNotFound()
{
    var ana = await SeedStudentAsync(TeacherA, "Ana");
    var result = await ServiceFor(TeacherB).GetAsync(ana.Id, CancellationToken.None);
    Assert.Equal(Error.NotFound("Student not found."), Assert.Single(result.Errors));
}
```

  Also: `Create_ValidRequest_SetsTeacherFromCurrentTeacher`, `Create_MissingEnglishLevel_ReturnsEnglishLevelRequired`, `Create_InvalidData_ReturnsDomainErrorsAndStoresNothing`, `List_ReturnsOnlyOwnStudentsSortedByName`, `List_SearchIgnoresCase` (`"ANA"` finds `"Ana"` and `"Mariana"`), `List_BlankSearch_ReturnsAll`, `List_IncludesLessonPlanCount`, `Update_StudentOfAnotherTeacher_ReturnsNotFound`, `Update_InvalidData_ReturnsValidationAndKeepsStored`, `Delete_StudentOfAnotherTeacher_ReturnsNotFound`, `Delete_NoLessonPlans_Removes`, `Create_DuplicateNameAndEmail_IsAccepted`.
  `LayerDependencyTests.ApplicationAssembly_ReferencesOnlyDomain`: referenced `LessonOrganizer.*` assemblies are exactly `LessonOrganizer.Domain`; none starts with `Microsoft.AspNetCore` or `Microsoft.EntityFrameworkCore`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Application.Tests` → build fails, `StudentService` not found. Commit `test: student use cases`.
- [ ] **Step 3: Implement ports, records and `StudentService`.** Service flow: guard → load owned → domain call → repository → map. `DeleteAsync` checks `CountByStudentsAsync` first (business rule, gives the Conflict message), then `TryDeleteAsync` (integrity guard; false also maps to the same Conflict).
- [ ] **Step 4: Run to verify they pass.** Same command → all passed.
- [ ] **Step 5: Commit** `feat: student use cases`.

---

### Task 6: `LessonPlanService` (stage 3)

**Files:**
- Create in `src/LessonOrganizer.Application/LessonPlans/`: `LessonPlanService.cs`, `LessonPlanRequest.cs`, `LessonPlanResponse.cs`, `ChangeLessonPlanStatusRequest.cs`
- Test: `tests/LessonOrganizer.Application.Tests/LessonPlans/LessonPlanServiceTests.cs`

**Interfaces:**
- Consumes: Task 5 ports and fakes.
- Produces:
  - `sealed record LessonPlanRequest(Guid? StudentId, string? Title, string? Description, DateOnly? LessonDate)`
  - `sealed record ChangeLessonPlanStatusRequest(LessonPlanStatus? Status)`
  - `sealed record LessonPlanResponse(Guid Id, Guid StudentId, string StudentName, string Title, string? Description, DateOnly LessonDate, LessonPlanStatus Status)` with `static LessonPlanResponse From(LessonPlan plan, string studentName)`
  - `sealed class LessonPlanService(ILessonPlanRepository lessonPlans, IStudentRepository students, ICurrentTeacher teacher)` — `ListAsync(LessonPlanFilter filter, ct) → Task<IReadOnlyList<LessonPlanResponse>>`; `GetAsync(Guid id, ct)`, `CreateAsync(LessonPlanRequest, ct)`, `UpdateAsync(Guid id, LessonPlanRequest, ct)`, `ChangeStatusAsync(Guid id, ChangeLessonPlanStatusRequest, ct)` → `Task<Result<LessonPlanResponse>>`; `DeleteAsync(Guid id, ct) → Task<Result>`
- Exact messages: `"Student not found."` as **Validation** on field `studentId` for a missing, foreign or null student (plan A3); `"Lesson date is required."` (field `lessonDate`); `"Status is required."` (field `status`); `"Lesson plan not found."` (NotFound).

- [ ] **Step 1: Write the failing tests.**

```csharp
[Fact]
public async Task Create_StudentOfAnotherTeacher_ReturnsSameErrorAsMissingStudent()
{
    var bStudent = await SeedStudentAsync(TeacherB, "Bob");
    var foreign = await ServiceFor(TeacherA).CreateAsync(Request(bStudent.Id), CancellationToken.None);
    var missing = await ServiceFor(TeacherA).CreateAsync(Request(Guid.CreateVersion7()), CancellationToken.None);
    Assert.Equal(Error.Validation("studentId", "Student not found."), Assert.Single(foreign.Errors));
    Assert.Equal(foreign.Errors, missing.Errors);
}

[Fact]
public async Task List_FilterByAnotherTeachersStudent_ReturnsEmpty()
{
    var bStudent = await SeedStudentAsync(TeacherB, "Bob");
    await SeedLessonPlanAsync(bStudent.Id, "B's plan");
    var list = await ServiceFor(TeacherA).ListAsync(new LessonPlanFilter(bStudent.Id, null), CancellationToken.None);
    Assert.Empty(list);
}
```

  Also: `Create_ValidRequest_StartsAsToPrepareWithStudentName`, `Create_MissingDate_ReturnsLessonDateRequired`, `Create_MissingStudentId_ReturnsStudentNotFound`, `Update_MoveToAnotherOwnStudent_Succeeds`, `Update_MoveToForeignStudent_ReturnsStudentNotFound`, `Update_DoesNotChangeStatus`, `Update_PlanOfAnotherTeacher_ReturnsNotFound`, `ChangeStatus_AnyToAny_Succeeds` (Delivered → ToPrepare), `ChangeStatus_MissingStatus_ReturnsStatusRequired`, `ChangeStatus_PlanOfAnotherTeacher_ReturnsNotFound`, `List_SortedByDateThenTitle`, `List_StudentAndStatusFiltersCombine`, `Delete_PlanOfAnotherTeacher_ReturnsNotFound`, `Delete_OwnPlan_Removes`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Application.Tests` → build fails, `LessonPlanService` not found. Commit `test: lesson plan use cases`.
- [ ] **Step 3: Implement.** `ListAsync` loads the teacher's students once (`ListByTeacherAsync(teacherId, null)`) to get names, then maps.
- [ ] **Step 4: Run to verify they pass.** Same command → all passed.
- [ ] **Step 5: Commit** `feat: lesson plan use cases`.

---

### Task 7: Database schema with EF Core (stage 4)

**Files:**
- Create in `src/LessonOrganizer.Infrastructure/`: `Identity/ApplicationUser.cs`; `Persistence/LessonOrganizerDbContext.cs`, `Persistence/DesignTimeDbContextFactory.cs`, `Persistence/Configurations/StudentConfiguration.cs`, `LessonPlanConfiguration.cs`, `ApplicationUserConfiguration.cs`; `Persistence/Migrations/*` (generated)
- Create at the root: `docker-compose.yml` (service `db` only), `.env.example`, `.config/dotnet-tools.json`
- Modify: `Directory.Packages.props`, `src/LessonOrganizer.Infrastructure/LessonOrganizer.Infrastructure.csproj`, `tests/LessonOrganizer.Infrastructure.Tests/LessonOrganizer.Infrastructure.Tests.csproj`, `docs/stack.md` (versions and image tag), `CLAUDE.md` (Commands: migrations, database)
- Test: `tests/LessonOrganizer.Infrastructure.Tests/Persistence/SqlServerFixture.cs`, `TestDatabase.cs`, `SchemaTests.cs`

**Interfaces:**
- Consumes: Tasks 3–4 entities.
- Produces:
  - `sealed class ApplicationUser : IdentityUser<Guid>` with `string Name { get; set; } = ""`
  - `sealed class LessonOrganizerDbContext(DbContextOptions<LessonOrganizerDbContext> options) : IdentityUserContext<ApplicationUser, Guid>(options)` — `DbSet<Student> Students`, `DbSet<LessonPlan> LessonPlans`; `OnModelCreating` calls the base, then applies the configurations from this assembly.
  - Configurations (values from `plan.md` §3): tables `Students`, `LessonPlans`; keys `ValueGeneratedNever`; `Name` 150, `Email` 254, `Notes` 2000, `Title` 150, `Description` `nvarchar(max)`; enums `HasConversion<string>().HasMaxLength(20)`; `LessonDate` `date`; `Students.TeacherId` → `AspNetUsers.Id` and `LessonPlans.StudentId` → `Students.Id`, both `OnDelete(DeleteBehavior.NoAction)`, no navigation properties on the domain entities; indexes `(TeacherId, Name)` and `(StudentId, LessonDate)`; `ApplicationUser.Name` max 150, required.
  - `DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LessonOrganizerDbContext>` — uses the `ConnectionStrings__LessonOrganizer` environment variable, or a password-less placeholder (`Server=localhost;Database=LessonOrganizer;Integrated Security=false;TrustServerCertificate=true`) when it is not set; `migrations add` never connects.
  - Test helpers: `SqlServerFixture` (xUnit v3 assembly fixture, `[assembly: AssemblyFixture(typeof(SqlServerFixture))]`) starts one `MsSqlContainer` for the run and exposes `string ConnectionStringFor(string database)`; `TestDatabase : IAsyncDisposable` creates a database named `test_<guid>`, applies the migrations, exposes `LessonOrganizerDbContext NewContext()`, and drops the database on dispose.
- `docker-compose.yml` `db` service as in `docs/stack.md` "Docker" (port on `127.0.0.1`, `mssql-data` volume, health check, `MSSQL_SA_PASSWORD` from `.env`). `.env.example`: `MSSQL_SA_PASSWORD=` with a line saying SQL Server needs at least 8 characters from three of: upper case, lower case, digits, symbols.

- [ ] **Step 1: Human checkpoint — Docker and versions.** Ask the human to start Docker Desktop and to pull (or approve pulling) `mcr.microsoft.com/mssql/server:2022-latest`; record the tag used. Take versions of `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design` (10.0.x), `Testcontainers.MsSql` and `dotnet-ef` (10.0.x) from `%USERPROFILE%\.nuget\packages`; ask the human for any that are missing. Add them to `Directory.Packages.props`; `Design` with `PrivateAssets=all` in Infrastructure; `Testcontainers.MsSql` in `Infrastructure.Tests`. Infrastructure gets `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. `dotnet new tool-manifest` and `dotnet tool install dotnet-ef --version <version>`.
- [ ] **Step 2: Write the failing tests** in `SchemaTests` (each test with its own `TestDatabase`).

```csharp
[Fact]
public async Task SaveLessonPlan_StudentMissing_ViolatesForeignKey()
{
    await using var db = await TestDatabase.CreateAsync(_sql);
    await using var context = db.NewContext();
    context.LessonPlans.Add(LessonPlan.Create(Guid.CreateVersion7(), "Orphan", null, Today).Value);
    var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    Assert.Equal(547, Assert.IsType<SqlException>(ex.InnerException).Number);
}

[Fact]
public async Task Model_MatchesLatestMigration()
{
    await using var db = await TestDatabase.CreateAsync(_sql);
    await using var context = db.NewContext();
    Assert.False(context.Database.HasPendingModelChanges());
}
```

  Also: `Migrate_EmptyDatabase_CreatesUserStudentAndLessonPlanTables` (`AspNetUsers`, `Students`, `LessonPlans` exist; `AspNetRoles` does not, read from `INFORMATION_SCHEMA.TABLES`), `DeleteStudent_WithLessonPlans_ViolatesForeignKey` (547), `SaveUsers_SameNormalizedUserName_ViolatesUniqueIndex` (2601), `SaveStudent_StoresEnglishLevelAsName` (raw `SELECT EnglishLevel` returns `"Intermediate"`), `SaveStudent_ThenNewContext_ReadsSameValues` (all fields, `LessonDate` for a plan round-trips as the same `DateOnly`).
- [ ] **Step 3: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Infrastructure.Tests` → build fails, `LessonOrganizerDbContext` not found. Commit `test: database schema`.
- [ ] **Step 4: Implement** `ApplicationUser`, the context, the configurations and the design-time factory per Interfaces.
- [ ] **Step 5: Create the migration.** `dotnet tool restore`, then `dotnet ef migrations add InitialCreate --project src/LessonOrganizer.Infrastructure --output-dir Persistence/Migrations`. Read the generated migration: no cascade deletes, no role tables, column sizes as in `plan.md` §3.
- [ ] **Step 6: Run to verify they pass.** `dotnet test tests/LessonOrganizer.Infrastructure.Tests` → all passed (Docker running).
- [ ] **Step 7: `docker-compose.yml` and `.env.example`.** Copy `.env.example` to `.env`, set a password, run `docker compose up -d db`, then `docker compose ps` → `db` is `healthy`. `docker compose down`.
- [ ] **Step 8: Update `docs/stack.md`** with the package versions and the image tag, and `CLAUDE.md` Commands with the migration and database rows.
- [ ] **Step 9: Commit** `feat: sql server schema with ef core migrations`.

---

### Task 8: EF Core repositories (stage 4)

**Files:**
- Create in `src/LessonOrganizer.Infrastructure/`: `Repositories/EfStudentRepository.cs`, `EfLessonPlanRepository.cs`, `Repositories/SqlErrors.cs`, `Repositories/LikePattern.cs`, `InfrastructureServiceCollectionExtensions.cs`
- Test: `tests/LessonOrganizer.Infrastructure.Tests/Repositories/EfStudentRepositoryTests.cs`, `EfLessonPlanRepositoryTests.cs`, `LikePatternTests.cs`

**Interfaces:**
- Consumes: Task 5 repository interfaces (exact signatures and sorting rules there), Task 7 context and test helpers.
- Produces:
  - `EfStudentRepository(LessonOrganizerDbContext db) : IStudentRepository`, `EfLessonPlanRepository(LessonOrganizerDbContext db) : ILessonPlanRepository`
  - `static class LikePattern` — `string Contains(string text)` returns `%` + text with `\`, `%`, `_`, `[` each prefixed by `\` + `%`; used with `EF.Functions.Like(column, pattern, "\\")`.
  - `static class SqlErrors` — `bool IsForeignKeyViolation(DbUpdateException ex)` (number 547), `bool IsDuplicateKey(DbUpdateException ex)` (2601, 2627).
  - `static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)` — registers the context (SQL Server provider) and the two repositories as scoped.
- Behavior: `TryDeleteAsync` returns false when the student has lesson plans, by checking first and by catching a foreign key violation; `AddAsync`/`UpdateAsync` of a lesson plan whose student is gone throw `InvalidOperationException("Lesson plan {id} references student {studentId}, which does not exist. Expected an existing student id.")` (from the foreign key violation). `UpdateAsync` of a missing id throws `InvalidOperationException` naming the id. Lesson plan ownership: join `Students` on `StudentId` and match `TeacherId`.

- [ ] **Step 1: Write the failing tests** (each test: own `TestDatabase`; writes and reads use different context instances).
  `LikePatternTests`: `Contains_EscapesWildcards` (`"50%_[x]\\"` → `"%50\\%\\_\\[x]\\\\%"`).
  Students: `Add_ThenNewContext_ReadsStudent`, `ListByTeacher_ReturnsOnlyOwnSortedByNameIgnoringCase` (`"bruno"`, `"Ana"`, `"Carla"` → Ana, bruno, Carla), `ListByTeacher_SearchIgnoresCase`, `ListByTeacher_SearchWithWildcards_MatchesLiterally` (students `"50% off"`, `"5000 off"`, `"a_b"`, `"axb"`; search `"50%"` → only `"50% off"`; `"a_b"` → only `"a_b"`), `GetOwned_OtherTeacher_ReturnsNull`, `TryDelete_WithLessonPlans_ReturnsFalseAndKeepsStudent`, `TryDelete_WithoutPlans_Removes`, `Update_ThenNewContext_ReadsChanges`.
  Lesson plans: `ListByTeacher_SortsByDateThenTitleThenId`, `ListByTeacher_FiltersCombine`, `GetOwned_ThroughOtherTeachersStudent_ReturnsNull`, `Add_StudentMissing_ThrowsNamingIds`, `CountByStudents_ReturnsZeroForStudentsWithoutPlans`, `Delete_Removes`.
  Students need a teacher row: tests insert an `ApplicationUser` through the context first.
- [ ] **Step 2: Run to verify they fail.** `dotnet test tests/LessonOrganizer.Infrastructure.Tests` → build fails, `EfStudentRepository` not found. Commit `test: ef core repositories`.
- [ ] **Step 3: Implement** the repositories, `LikePattern`, `SqlErrors` and `AddInfrastructure` per Interfaces. Reads use `AsNoTracking`; updates attach the entity and save.
- [ ] **Step 4: Run to verify they pass.** Same command → all passed.
- [ ] **Step 5: Run the whole suite.** `dotnet test` (Docker running) → all projects passed, 0 warnings. `dotnet test --collect:"XPlat Code Coverage"` → coverage files produced; record the line coverage of Domain, Application, Infrastructure in `docs/ai/AI_LOG.md`.
- [ ] **Step 6: Commit** `feat: ef core repositories`.
