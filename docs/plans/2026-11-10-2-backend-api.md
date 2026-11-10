# Backend API Implementation Plan (stages 5–8)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The HTTP API on top of plan 1: register, login with lockout, logout and session (`/api/auth`), students and lesson plans endpoints with per-teacher isolation, migrations and demo data at startup, and an OpenAPI document with the Scalar UI in Development.

**Architecture:** Application gets `AuthService` and the `IUserAccounts` port; Infrastructure implements it with ASP.NET Core Identity's `UserManager`; Api holds controllers that call one service method each and map `Result` to HTTP with `ResultHttpMapping`. Cookies, antiforgery, rate limiting and the default-deny policy are configured in `Program.cs`. Controller unit tests use the real services with in-memory fakes; integration tests run the whole pipeline with `WebApplicationFactory` against a Testcontainers SQL Server.

**Tech Stack:** .NET 10, ASP.NET Core controllers, ASP.NET Core Identity (EF Core stores), EF Core 10 SQL Server, xUnit v3 on Microsoft Testing Platform, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`.

**Spec:** [`docs/plan.md`](../plan.md) (§2, §3 seed data, §4 authentication approach, §5 API contracts, §6 acceptance criteria), [`docs/architecture.md`](../architecture.md), [`docs/stack.md`](../stack.md), [ADR 0003](../decisions/0003-sql-server-ef-core-docker.md). Exercise statement: `docs/Net - BLA - Technical Interview Exercise - V6 (1).pdf` (local only). Builds on [plan 1](2026-11-10-1-backend-core.md), merged on `main`.

**Plan series** ([`plan.md` §7](../plan.md#7-implementation-stages)): 1 Backend core (done) · 2 Backend API (this) · 3 Frontend (stages 9–11) · 4 Delivery (stage 12) · 5 Security review (stage 13).

## Global Constraints

- Everything from plan 1's Global Constraints still applies (SDK, build settings, no comments, method size, test naming, TDD commits, AI_LOG entry per task, no feed lookups beyond what the human approved).
- New packages and versions (human-approved on 2026-11-10): `Microsoft.AspNetCore.Mvc.Testing` 10.0.9 (cache), `Microsoft.AspNetCore.OpenApi` 10.0.12 (cache), `Scalar.AspNetCore` 2.20.0 (nuget.org, approved by the human). Missing transitive packages restore from nuget.org only (`nuget.config`).
- Teacher id only from the `NameIdentifier` claim through `ICurrentTeacher`; no request record has a teacher id.
- Another teacher's resource → 404, exactly like a missing one. Another teacher's or a missing referenced student → 400 on `studentId` with "Student not found.".
- Every endpoint requires a session (fallback policy) except `GET /api/auth/csrf`, `POST /api/auth/register`, `POST /api/auth/login`, and the OpenAPI/Scalar endpoints, which exist only in Development.
- Unsafe methods (POST, PUT, PATCH, DELETE) validate the antiforgery token from the `X-XSRF-TOKEN` header, `login` and `register` included.
- Auth cookie: `HttpOnly`, `SecurePolicy.Always`, `SameSite=Strict`, 8 h sliding expiration; unauthenticated → 401, never a redirect.
- Lockout: 5 failed logins → 15 minutes. Wrong password, unknown email and locked account all return 401 with "Invalid email or password.".
- JSON: enums as strings, `JsonStringEnumConverter(allowIntegerValues: false)`; `lessonDate` as `yyyy-MM-dd`.
- Errors as RFC 9457 ProblemDetails; no stack traces or exception messages in responses outside Development.
- Integration tests use `https://localhost` as base address (the auth cookie is `Secure`).

## Review Focus

1. **Emails Identity's default user-name rules reject** (`o'neil@example.com`, `ana+work@example.com`): `UserName` is the email, so registration must still succeed. Pinned in Task 3 (`AllowedUserNameCharacters` emptied).
2. **Login with the email in another letter case** (`MARIA@EXAMPLE.COM`): must succeed. Pinned in Task 3.
3. **Integer enum values in JSON bodies** (`"englishLevel": 3`, `"status": 1`): 400, nothing stored. Pinned in Task 6 and Task 7.
4. **Undefined status in the query string** (`?status=7`, `?status=Done`): 400, not an empty list. Pinned in Task 7 (closes plan 1's deferred minor).
5. **Malformed JSON and non-GUID ids** (`{ "name": `, `/api/students/abc`): 400 ProblemDetails without internal details, and 404. Pinned in Task 6.

---

### Task 1: Shared test library

Moves the in-memory fakes and the SQL Server fixture out of single test projects so Application, Api and Infrastructure tests share them. No behavior change.

**Files:**
- Create: `tests/LessonOrganizer.Testing/LessonOrganizer.Testing.csproj`
- Move: `tests/LessonOrganizer.Application.Tests/Fakes/*.cs` (except `InMemoryRepositoryTests.cs`) → `tests/LessonOrganizer.Testing/Fakes/`, namespace `LessonOrganizer.Testing.Fakes`
- Move: `tests/LessonOrganizer.Infrastructure.Tests/Persistence/SqlServerFixture.cs` → `tests/LessonOrganizer.Testing/SqlServer/SqlServerFixture.cs`, namespace `LessonOrganizer.Testing.SqlServer`; the `[assembly: AssemblyFixture(typeof(SqlServerFixture))]` line moves to `tests/LessonOrganizer.Infrastructure.Tests/AssemblyFixtures.cs`
- Modify: `tests/Directory.Build.props`, `LessonOrganizer.slnx`, the four test `.csproj` files, `Directory.Packages.props` (`Testcontainers.MsSql` reference moves to `LessonOrganizer.Testing`)

**Interfaces:**
- Produces: project `LessonOrganizer.Testing` (class library, not a test project) referencing `LessonOrganizer.Application` and `Testcontainers.MsSql`; referenced by all four other test projects except `Domain.Tests`. Types keep their names: `InMemoryData`, `InMemoryStudentRepository`, `InMemoryLessonPlanRepository`, `FixedCurrentTeacher`, `EntityCopy`, `SqlServerFixture`.
- `tests/Directory.Build.props`: test settings (`IsTestProject`, `OutputType Exe`, `xunit.v3`) apply only when `'$(MSBuildProjectName)' != 'LessonOrganizer.Testing'`; `LessonOrganizer.Testing` references `xunit.v3.extensibility.core` (version of the `xunit.v3` 4.0.1 family, from the cache) for `IAsyncLifetime`.

- [ ] **Step 1: Create the library and move the files** as listed; fix namespaces and `using`s.
- [ ] **Step 2: Verify.** `dotnet build` → 0 warnings. `dotnet test` → 101 passed (same as before), exit 8 only from the two empty Api test projects.
- [ ] **Step 3: Commit** `refactor: share fakes and sql server fixture in a test library`.

---

### Task 2: `AuthService` and the `IUserAccounts` port (stage 5)

**Files:**
- Modify: `src/LessonOrganizer.Domain/Common/ErrorKind.cs`, `Error.cs`
- Create in `src/LessonOrganizer.Application/`: `Abstractions/IUserAccounts.cs`; `Auth/AuthService.cs`, `RegisterRequest.cs`, `LoginRequest.cs`, `UserResponse.cs`, `LoginOutcome.cs`
- Create: `tests/LessonOrganizer.Testing/Fakes/InMemoryUserAccounts.cs`
- Test: `tests/LessonOrganizer.Domain.Tests/Common/ResultTests.cs` (one test), `tests/LessonOrganizer.Application.Tests/Auth/AuthServiceTests.cs`

**Interfaces:**
- Produces:
  - `ErrorKind.Unauthorized`; `static Error Error.Unauthorized(string message)`
  - `sealed record RegisterRequest(string? Name, string? Email, string? Password)`
  - `sealed record LoginRequest(string? Email, string? Password)`
  - `sealed record UserResponse(Guid Id, string Name, string Email)`
  - `enum LoginStatus { Succeeded, InvalidCredentials, LockedOut }`; `sealed record LoginOutcome(LoginStatus Status, UserResponse? User)`
  - `interface IUserAccounts` — `Task<Result<UserResponse>> CreateAsync(string name, string email, string password, CancellationToken ct)` (Conflict when the email exists in any letter case); `Task<UserResponse?> FindByIdAsync(Guid id, CancellationToken ct)`; `Task<LoginOutcome> CheckPasswordAsync(string email, string password, CancellationToken ct)` (counts failures and applies lockout)
  - `sealed class AuthService(IUserAccounts accounts)` — `RegisterAsync(RegisterRequest, ct) → Task<Result<UserResponse>>`; `LoginAsync(LoginRequest, ct) → Task<Result<UserResponse>>`; `GetAsync(Guid userId, ct) → Task<Result<UserResponse>>`
  - Fake `InMemoryUserAccounts` — stores name, email, plain password and failure count; same Conflict rule (case-insensitive); locks after 5 failures until `Unlock(email)` is called; exposes `IReadOnlyList<UserResponse> Users`.
- Rules and exact messages:
  - Register: name via `TextField.Required(name, "name", "Name", 150)`; email via `EmailField.Required`; password `"Password is required."`, `"Password must be at least 8 characters."`, `"Password must be at most 128 characters."` (field `password`; length counted untrimmed, never trimmed). All errors collected; nothing created when any.
  - Duplicate email: `Error.Conflict("An account with this email already exists.")`.
  - Login: blank email → `Error.Validation("email", "Email is required.")`, blank password → `Error.Validation("password", "Password is required.")`; `InvalidCredentials` and `LockedOut` → `Error.Unauthorized("Invalid email or password.")`. Email trimmed before the check.
  - `GetAsync` of a missing user → `Error.NotFound("User not found.")`.

- [ ] **Step 1: Write the failing tests.**

```csharp
[Fact]
public async Task Login_LockedOut_ReturnsSameErrorAsWrongPassword()
{
    await _accounts.CreateAsync("Maria", "maria@example.com", "Demo1234!", Ct);
    var wrong = await _service.LoginAsync(new LoginRequest("maria@example.com", "wrong-pass"), Ct);
    for (var i = 0; i < 4; i++) await _service.LoginAsync(new LoginRequest("maria@example.com", "wrong-pass"), Ct);
    var locked = await _service.LoginAsync(new LoginRequest("maria@example.com", "Demo1234!"), Ct);
    Assert.Equal(Error.Unauthorized("Invalid email or password."), Assert.Single(wrong.Errors));
    Assert.Equal(wrong.Errors, locked.Errors);
}

[Fact]
public async Task Register_PasswordOf7Chars_ReturnsPasswordErrorAndCreatesNothing()
{
    var result = await _service.RegisterAsync(new RegisterRequest("Maria", "maria@example.com", "1234567"), Ct);
    Assert.Equal(Error.Validation("password", "Password must be at least 8 characters."), Assert.Single(result.Errors));
    Assert.Empty(_accounts.Users);
}
```

  Also: `Register_ValidRequest_ReturnsUser` (name and email trimmed), `Register_MissingNameInvalidEmailMissingPassword_ReturnsAllErrors` (fields `name`, `email`, `password`), `Register_PasswordOf129Chars_ReturnsMaxLengthError`, `Register_PasswordOf8Spaces_IsAccepted`, `Register_ExistingEmailOtherCase_ReturnsConflict`, `Login_ValidCredentials_ReturnsUser`, `Login_UnknownEmail_ReturnsUnauthorized`, `Login_BlankEmailAndPassword_ReturnsBothRequiredErrors`, `Get_MissingUser_ReturnsNotFound`. Domain: `Unauthorized_HasKindUnauthorized`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Application.Tests` → build fails, `AuthService` not found. Commit `test: auth use cases`.
- [ ] **Step 3: Implement** per Interfaces.
- [ ] **Step 4: Run to verify they pass.** `dotnet test --project tests/LessonOrganizer.Domain.Tests` and `--project tests/LessonOrganizer.Application.Tests` → all passed.
- [ ] **Step 5: Commit** `feat: auth use cases`.

---

### Task 3: `IdentityUserAccounts` over ASP.NET Core Identity (stage 5)

**Files:**
- Create: `src/LessonOrganizer.Infrastructure/Identity/IdentityUserAccounts.cs`
- Modify: `src/LessonOrganizer.Infrastructure/InfrastructureServiceCollectionExtensions.cs`, `docs/architecture.md` (Infrastructure bullet: lockout through `UserManager`, not `SignInManager.CheckPasswordSignInAsync`)
- Test: `tests/LessonOrganizer.Infrastructure.Tests/Identity/IdentityUserAccountsTests.cs`

**Interfaces:**
- Consumes: Task 2 `IUserAccounts`, `UserResponse`, `LoginOutcome`; plan 1 `LessonOrganizerDbContext`, `ApplicationUser`, `TestDatabase`, `SqlErrors`.
- Produces:
  - `sealed class IdentityUserAccounts(UserManager<ApplicationUser> users, IPasswordHasher<ApplicationUser> hasher) : IUserAccounts`
  - `AddInfrastructure` also calls `AddIdentityCore<ApplicationUser>(...)` with: `User.RequireUniqueEmail = true`, `User.AllowedUserNameCharacters = ""`, `Password.RequiredLength = 8`, `RequireDigit`/`RequireLowercase`/`RequireUppercase`/`RequireNonAlphanumeric = false`, `RequiredUniqueChars = 1`, `Lockout.MaxFailedAccessAttempts = 5`, `Lockout.DefaultLockoutTimeSpan = 15 min`, `Lockout.AllowedForNewUsers = true`; then `.AddEntityFrameworkStores<LessonOrganizerDbContext>().AddSignInManager()`; registers `IUserAccounts` → `IdentityUserAccounts` (scoped).
- Behavior:
  - `CreateAsync`: `new ApplicationUser { Name, Email = email, UserName = email }`, `users.CreateAsync(user, password)`. Identity errors `DuplicateUserName`/`DuplicateEmail`, or a duplicate-key `DbUpdateException` from a race, → `Error.Conflict("An account with this email already exists.")`; any other Identity error → `Error.Validation("password", <Identity description>)` for password codes, `"email"` otherwise.
  - `CheckPasswordAsync`: unknown email → verify the password against a fixed dummy hash (static, computed once with a random value), return `InvalidCredentials`; locked → verify the hash too, return `LockedOut`; right password → `ResetAccessFailedCountAsync`, `Succeeded`; wrong → `AccessFailedAsync`, then `LockedOut` if now locked, else `InvalidCredentials`.
- Test setup: `ServiceCollection().AddLogging().AddInfrastructure(db connection string)`, scope per call; one `TestDatabase` per test.

- [ ] **Step 1: Write the failing tests.**

```csharp
[Fact]
public async Task CheckPassword_FiveWrongThenRight_ReturnsLockedOut()
{
    await CreateAsync("maria@example.com", "Demo1234!");
    for (var i = 0; i < 5; i++) await CheckAsync("maria@example.com", "wrong-pass");
    Assert.Equal(LoginStatus.LockedOut, (await CheckAsync("maria@example.com", "Demo1234!")).Status);
}

[Fact]
public async Task Create_EmailWithApostrophe_Succeeds() =>
    Assert.True((await CreateAsync("o'neil@example.com", "Demo1234!")).IsSuccess);
```

  Also: `Create_ThenCheckPassword_Succeeds` (returns name and email), `Check_EmailInOtherCase_Succeeds` (`MARIA@EXAMPLE.COM`), `Create_SameEmailOtherCase_ReturnsConflict`, `Create_StoresHashNotPassword` (stored `PasswordHash` ≠ password and non-empty), `Create_PlusAddress_Succeeds` (`ana+work@example.com`), `CheckPassword_UnknownEmail_ReturnsInvalidCredentials`, `CheckPassword_FourWrongThenRight_ResetsCount` (then four wrong more → still `InvalidCredentials`), `FindById_ReturnsNameAndEmail`, `FindById_Missing_ReturnsNull`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Infrastructure.Tests` → build fails, `IdentityUserAccounts` not found. Commit `test: identity user accounts`.
- [ ] **Step 3: Implement** per Interfaces; update `architecture.md`.
- [ ] **Step 4: Run to verify they pass.** Same command (Docker running) → all passed.
- [ ] **Step 5: Commit** `feat: identity user accounts with lockout`.

---

### Task 4: HTTP mapping, current teacher and `AuthController` (stage 5)

**Files:**
- Create in `src/LessonOrganizer.Api/`: `Http/ResultHttpMapping.cs`; `Security/ClaimsCurrentTeacher.cs`, `Security/IAuthSession.cs`, `Security/IdentityAuthSession.cs`; `Controllers/AuthController.cs`
- Create: `tests/LessonOrganizer.Testing/Fakes/RecordingAuthSession.cs`
- Test: `tests/LessonOrganizer.Api.Tests/Http/ResultHttpMappingTests.cs`, `Security/ClaimsCurrentTeacherTests.cs`, `Controllers/AuthControllerTests.cs`

**Interfaces:**
- Consumes: Task 2 `AuthService`; `Result`, `Result<T>`, `Error`.
- Produces:
  - `static class ResultHttpMapping` — `IActionResult ToOk<T>(Result<T> result)` (200 `OkObjectResult`); `IActionResult ToCreated<T>(Result<T> result, string actionName, Func<T, object> routeValues)` (201 `CreatedAtActionResult`); `IActionResult ToNoContent(Result result)` (204). Failures, in this order: any `Validation` → 400 `ObjectResult` with `ValidationProblemDetails` whose `Errors` are keyed by `Field` (all validation errors); else the first error: `Unauthorized` → 401, `NotFound` → 404, `Conflict` → 409, each an `ObjectResult` with `ProblemDetails { Status, Title = "Unauthorized" | "Not Found" | "Conflict", Detail = message }`. Built without `HttpContext`, so controllers are unit-testable.
  - `sealed class ClaimsCurrentTeacher(IHttpContextAccessor accessor) : ICurrentTeacher` — `Id` parses the `ClaimTypes.NameIdentifier` claim; missing or not a Guid → `InvalidOperationException("The current user has no valid NameIdentifier claim (found '<value>'). Expected a Guid user id.")`.
  - `interface IAuthSession { Task SignInAsync(Guid userId); Task SignOutAsync(); }`; `IdentityAuthSession(SignInManager<ApplicationUser>, UserManager<ApplicationUser>)` signs in with `isPersistent: true`.
  - `RecordingAuthSession : IAuthSession` — records `SignedInUserId` and `SignedOut`.
  - `[ApiController, Route("api/auth")] sealed class AuthController(AuthService auth, IAuthSession session, ICurrentTeacher currentTeacher)`:
    - `POST register` `[AllowAnonymous]` → 201 `UserResponse` (`StatusCode(201, value)`; no session) or mapped error.
    - `POST login` `[AllowAnonymous]` → on success `session.SignInAsync(user.Id)`, 200 `UserResponse`; else mapped error (401 for bad credentials).
    - `POST logout` → `session.SignOutAsync()`, 204.
    - `GET me` → `auth.GetAsync(currentTeacher.Id)`; `NotFound` becomes 401 ProblemDetails (session for a user that no longer exists).
    - `GET csrf` `[AllowAnonymous]` → `IAntiforgery.GetAndStoreTokens(HttpContext)`, cookie `XSRF-TOKEN` = request token (`HttpOnly = false`, `Secure = true`, `SameSite = Strict`, `Path = "/"`), 204. Covered by integration tests only (needs `HttpContext`).

- [ ] **Step 1: Write the failing tests.** `ResultHttpMappingTests`: `Failure_ValidationErrors_Returns400WithFieldKeys` (two errors on `name` and `email` → `Errors["name"]`, `Errors["email"]`), `Failure_NotFound_Returns404ProblemWithDetail`, `Failure_Conflict_Returns409`, `Failure_Unauthorized_Returns401`, `ValidationWinsOverNotFound_Returns400`, `ToCreated_Success_Returns201WithRouteValues`, `ToNoContent_Success_Returns204`. `ClaimsCurrentTeacherTests`: `Id_ValidClaim_ReturnsGuid`, `Id_MissingClaim_ThrowsNamingExpectedShape`. `AuthControllerTests` (real `AuthService` + `InMemoryUserAccounts` + `RecordingAuthSession`):

```csharp
[Fact]
public async Task Login_WrongPassword_Returns401AndStartsNoSession()
{
    await _accounts.CreateAsync("Maria", "maria@example.com", "Demo1234!", Ct);
    var result = await _controller.Login(new LoginRequest("maria@example.com", "wrong-pass"), Ct);
    Assert.Equal(401, Assert.IsType<ObjectResult>(result).StatusCode);
    Assert.Null(_session.SignedInUserId);
}
```

  Also: `Register_Valid_Returns201AndStartsNoSession`, `Register_DuplicateEmail_Returns409`, `Register_ShortPassword_Returns400`, `Login_Valid_Returns200AndSignsIn`, `Logout_SignsOutAndReturns204`, `Me_ExistingUser_Returns200`, `Me_UserGone_Returns401`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Api.Tests` → build fails, `ResultHttpMapping` not found. Commit `test: http mapping and auth controller`.
- [ ] **Step 3: Implement** per Interfaces.
- [ ] **Step 4: Run to verify they pass.** Same command → all passed.
- [ ] **Step 5: Commit** `feat: http mapping and auth controller`.

---

### Task 5: Composition root and auth over HTTP (stage 5)

**Files:**
- Modify: `src/LessonOrganizer.Api/Program.cs`, `LessonOrganizer.Api.csproj` (`UserSecretsId`; `InternalsVisibleTo` not needed — add `public partial class Program;`), `appsettings.json`, `appsettings.Development.json`
- Create: `src/LessonOrganizer.Api/Startup/ApiServiceCollectionExtensions.cs` (`AddApi`), `Startup/DatabaseStartup.cs` (`MigrateIfConfiguredAsync`)
- Modify: `Directory.Packages.props`, `tests/LessonOrganizer.Api.IntegrationTests/LessonOrganizer.Api.IntegrationTests.csproj` (`Microsoft.AspNetCore.Mvc.Testing`, `LessonOrganizer.Testing`)
- Test: `tests/LessonOrganizer.Api.IntegrationTests/AssemblyFixtures.cs`, `ApiFactory.cs`, `ApiClient.cs`, `Auth/AuthEndpointsTests.cs`, `Auth/RateLimitTests.cs`; `tests/LessonOrganizer.Api.Tests/Security/AnonymousEndpointsTests.cs`

**Interfaces:**
- Consumes: Tasks 3–4; plan 1 `AddInfrastructure`.
- Produces:
  - Configuration keys: `ConnectionStrings:LessonOrganizer` (required; missing → `InvalidOperationException("Connection string 'LessonOrganizer' is missing. Set ConnectionStrings:LessonOrganizer (user secrets in development, environment variable in Docker).")`), `Database:MigrateOnStartup` (bool, default false; true in `appsettings.Development.json`), `RateLimiting:Auth:PermitLimit` (default 10) and `RateLimiting:Auth:WindowSeconds` (default 60).
  - `Program.cs`: JSON console logging; `AddInfrastructure`; `AddScoped<AuthService>`, `StudentService`, `LessonPlanService`; `AddHttpContextAccessor`; `ICurrentTeacher` → `ClaimsCurrentTeacher`; `IAuthSession` → `IdentityAuthSession`; `AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies()`; `ConfigureApplicationCookie` per Global Constraints, `Events.OnRedirectToLogin` → 401, `OnRedirectToAccessDenied` → 403; `AddAuthorization` with fallback policy `RequireAuthenticatedUser`; `AddAntiforgery(HeaderName = "X-XSRF-TOKEN")`; `AddControllers(options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))` with JSON options; `AddProblemDetails`; rate limiter policy `"auth"` (fixed window per remote IP, 429) applied with `[EnableRateLimiting("auth")]` on `register` and `login`; `UseExceptionHandler()` outside Development; `UseRateLimiter`, `UseAuthentication`, `UseAuthorization`; `MapControllers`; `await DatabaseStartup.MigrateIfConfiguredAsync(app)` before `Run`.
  - `ApiFactory(SqlServerFixture sql) : WebApplicationFactory<Program>` — environment `Testing`; new database `api_<guid>` per factory; settings `ConnectionStrings:LessonOrganizer`, `Database:MigrateOnStartup=true`; optional extra settings dictionary.
  - `ApiClient` — wraps an `HttpClient` from the factory (`BaseAddress = https://localhost`, cookies kept); `Task RefreshCsrfAsync()` (GET csrf, copy `XSRF-TOKEN` cookie to the `X-XSRF-TOKEN` header); `Task<HttpResponseMessage> RegisterAsync(name, email, password)`, `LoginAsync(email, password)` (refreshes the token after success), `LogoutAsync()`; `HttpClient Http`; `static Task<ApiClient> CreateAsync(ApiFactory factory)` (client with a fresh CSRF token); `static Task<ApiClient> LoggedInAsync(ApiFactory factory, string email)` (registers with password `Demo1234!` and logs in).

- [ ] **Step 1: Write the failing tests.** `AuthEndpointsTests` (one `ApiFactory` per test class):

```csharp
[Fact]
public async Task Login_Valid_SetsHttpOnlySecureStrictCookie()
{
    var client = await ApiClient.CreateAsync(_factory);
    await client.RegisterAsync("Maria", "maria@example.com", "Demo1234!");
    var response = await client.LoginAsync("maria@example.com", "Demo1234!");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Identity.Application="));
    Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task Register_WithoutXsrfToken_IsRejected()
{
    var http = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    var response = await http.PostAsJsonAsync("/api/auth/register", new { name = "Maria", email = "maria@example.com", password = "Demo1234!" });
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

  Also: `Register_Valid_Returns201ThenLoginWorks`, `Register_SameEmailOtherCase_Returns409`, `Me_WithoutSession_Returns401NotRedirect`, `Me_AfterLogin_ReturnsUser`, `Me_AfterLogout_Returns401`, `Login_WrongPassword_Returns401WithGenericMessage` (ProblemDetails `detail` = "Invalid email or password."), `Login_FiveWrongThenRight_Returns401`, `Login_EmailInOtherCase_Returns200`, `StoredUser_HasHashNotPassword` (read `AspNetUsers.PasswordHash` through a `LessonOrganizerDbContext` on the factory's database). `RateLimitTests` (factory with `RateLimiting:Auth:PermitLimit=3`): `Login_FourthAttemptInWindow_Returns429`. `AnonymousEndpointsTests` (reflection over the Api assembly's controller actions): `AnonymousActions_AreOnlyCsrfRegisterLogin`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Api.IntegrationTests` → build fails, `Program` has no public type / `ApiFactory` dependencies missing. Commit `test: auth over http`.
- [ ] **Step 3: Implement** `Program.cs`, `AddApi`, `DatabaseStartup`, settings; `dotnet user-secrets init --project src/LessonOrganizer.Api`.
- [ ] **Step 4: Run to verify they pass.** `dotnet test --project tests/LessonOrganizer.Api.IntegrationTests` and `--project tests/LessonOrganizer.Api.Tests` → all passed.
- [ ] **Step 5: Manual check.** `docker compose up -d db`; `dotnet user-secrets set ConnectionStrings:LessonOrganizer "Server=localhost,1433;Database=LessonOrganizer;User Id=sa;Password=<.env value>;TrustServerCertificate=true" --project src/LessonOrganizer.Api`; `dotnet run --project src/LessonOrganizer.Api --launch-profile https` → starts, database created; `GET https://localhost:7141/api/auth/me` → 401. Stop it.
- [ ] **Step 6: Commit** `feat: api host with cookie auth, antiforgery and rate limiting`.

---

### Task 6: Students API (stage 6)

**Files:**
- Create: `src/LessonOrganizer.Api/Controllers/StudentsController.cs`
- Test: `tests/LessonOrganizer.Api.Tests/Controllers/StudentsControllerTests.cs`, `tests/LessonOrganizer.Api.IntegrationTests/Students/StudentsEndpointsTests.cs`

**Interfaces:**
- Consumes: plan 1 `StudentService`, `StudentRequest`, `StudentResponse`; Task 4 `ResultHttpMapping`; Task 5 `ApiFactory`, `ApiClient`.
- Produces: `[ApiController, Route("api/students")] sealed class StudentsController(StudentService students)` — `GET ?search=` → 200 list; `GET {id:guid}` (action name `Get`) → 200/404; `POST` → 201 + `Location` to `Get`; `PUT {id:guid}` → 200/400/404; `DELETE {id:guid}` → 204/404/409. Each action calls one service method and maps the result.

- [ ] **Step 1: Write the failing tests.** Unit (real `StudentService`, fakes, `FixedCurrentTeacher`): `Create_Valid_Returns201WithLocationRouteId`, `Create_Invalid_Returns400WithFieldErrors`, `Get_OtherTeachersStudent_Returns404`, `Update_Valid_Returns200`, `Delete_WithLessonPlans_Returns409`, `Delete_NoPlans_Returns204`, `List_ReturnsOwnStudents`. Integration (two logged-in teachers A and B):

```csharp
[Fact]
public async Task TeacherB_GetsNotFoundOnEveryOperationOnTeacherAsStudent()
{
    var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
    var b = await ApiClient.LoggedInAsync(_factory, "b@example.com");
    var id = await a.CreateStudentAsync("Ana");
    Assert.Equal(HttpStatusCode.NotFound, (await b.Http.GetAsync($"/api/students/{id}")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await b.Http.PutAsJsonAsync($"/api/students/{id}", Student("Hacked"))).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await b.Http.DeleteAsync($"/api/students/{id}")).StatusCode);
    Assert.Empty(await b.Http.GetFromJsonAsync<List<JsonElement>>("/api/students?search=Ana"));
}
```

  Also: `Create_ThenGet_ReturnsCamelCaseJsonWithEnumName` (body has `"englishLevel":"Intermediate"` and `lessonPlanCount`), `Create_IntegerEnglishLevel_Returns400AndStoresNothing` (`"englishLevel": 3`), `Create_MalformedJson_Returns400ProblemWithoutStackTrace` (body `{ "name": `; response JSON has no `exception`/`stackTrace` text), `Get_NonGuidId_Returns404`, `Endpoints_WithoutSession_Return401` (GET list, POST), `Search_IgnoresCase`, `Delete_WithLessonPlans_Returns409WithMessage` (detail = plan 1 Conflict message; the plan is created through `POST /api/lesson-plans` in Task 7, so here insert it through a `LessonOrganizerDbContext` on the factory's database), `Data_SurvivesNewFactoryOnSameDatabase` (second `ApiFactory` with the same connection string reads the student).
  `ApiClient` gains `Task<Guid> CreateStudentAsync(string name)` (asserts 201).
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Api.Tests` → build fails, `StudentsController` not found. Commit `test: students api`.
- [ ] **Step 3: Implement** the controller.
- [ ] **Step 4: Run to verify they pass.** `dotnet test --project tests/LessonOrganizer.Api.Tests` and `--project tests/LessonOrganizer.Api.IntegrationTests` → all passed.
- [ ] **Step 5: Commit** `feat: students api`.

---

### Task 7: Lesson plans API (stage 7)

**Files:**
- Create: `src/LessonOrganizer.Api/Controllers/LessonPlansController.cs`
- Modify: `src/LessonOrganizer.Application/LessonPlans/LessonPlanService.cs` (`ListAsync` returns `Result`), its tests
- Test: `tests/LessonOrganizer.Api.Tests/Controllers/LessonPlansControllerTests.cs`, `tests/LessonOrganizer.Api.IntegrationTests/LessonPlans/LessonPlansEndpointsTests.cs`, `tests/LessonOrganizer.Application.Tests/LessonPlans/LessonPlanServiceTests.cs`

**Interfaces:**
- Consumes: plan 1 `LessonPlanService` and records; Tasks 4–6 helpers.
- Produces:
  - `LessonPlanService.ListAsync(LessonPlanFilter filter, ct) → Task<Result<IReadOnlyList<LessonPlanResponse>>>`; an undefined `filter.Status` → `Error.Validation("status", "Status must be one of: ToPrepare, Ready, Delivered.")`.
  - `[ApiController, Route("api/lesson-plans")] sealed class LessonPlansController(LessonPlanService lessonPlans)` — `GET ?studentId=&status=` → 200/400; `GET {id:guid}` (action `Get`) → 200/404; `POST` → 201 + `Location`; `PUT {id:guid}` → 200/400/404; `PATCH {id:guid}/status` body `ChangeLessonPlanStatusRequest` → 200/400/404; `DELETE {id:guid}` → 204/404.

- [ ] **Step 1: Write the failing tests.** Application: `List_UndefinedStatus_ReturnsStatusError` (existing list tests switch to `.Value`). Unit: `Create_ForeignStudent_Returns400OnStudentId`, `ChangeStatus_Valid_Returns200WithNewStatus`, `ChangeStatus_OtherTeachersPlan_Returns404`, `List_UndefinedStatus_Returns400`, `Delete_Own_Returns204`. Integration:

```csharp
[Fact]
public async Task List_NumericUndefinedStatusInQuery_Returns400()
{
    var a = await ApiClient.LoggedInAsync(_factory, "a@example.com");
    Assert.Equal(HttpStatusCode.BadRequest, (await a.Http.GetAsync("/api/lesson-plans?status=7")).StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, (await a.Http.GetAsync("/api/lesson-plans?status=Done")).StatusCode);
}
```

  Also: `Create_Returns201ToPrepareWithStudentName`, `Create_DateAsIsoString_RoundTrips` (`"lessonDate":"2026-11-12"` in and out), `Create_OtherTeachersStudent_Returns400SameAsMissing` (both bodies' `errors.studentId` = `["Student not found."]`), `ChangeStatus_IntegerStatusInBody_Returns400` (`{ "status": 1 }`), `ChangeStatus_AnyToAny_Returns200`, `Update_DoesNotChangeStatus`, `TeacherB_GetsNotFoundOnEveryOperationOnTeacherAsPlan` (GET, PUT, PATCH, DELETE), `List_FilterByOtherTeachersStudent_ReturnsEmpty`, `List_SortedByDateAndFiltersCombine`, `DeleteStudent_WithPlanCreatedOverHttp_Returns409`.
  `ApiClient` gains `Task<Guid> CreateLessonPlanAsync(Guid studentId, string title, string lessonDate)`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Application.Tests` → build fails (`.Value` on a list); `--project tests/LessonOrganizer.Api.Tests` → `LessonPlansController` not found. Commit `test: lesson plans api`.
- [ ] **Step 3: Implement** the service change and the controller.
- [ ] **Step 4: Run to verify they pass.** Application, Api and Api.IntegrationTests projects → all passed.
- [ ] **Step 5: Commit** `feat: lesson plans api`.

---

### Task 8: Demo data seed (stage 8)

**Files:**
- Create: `src/LessonOrganizer.Infrastructure/Seed/DemoDataSeeder.cs`
- Modify: `src/LessonOrganizer.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (register seeder and `TimeProvider.System`), `src/LessonOrganizer.Api/Startup/DatabaseStartup.cs` (seed after migrations when `Seed:DemoData` is true), `appsettings.Development.json` (`Seed:DemoData: true`), `README.md` ("Demo credentials" section)
- Create: `tests/LessonOrganizer.Testing/FixedTimeProvider.cs`
- Test: `tests/LessonOrganizer.Infrastructure.Tests/Seed/DemoDataSeederTests.cs`, `tests/LessonOrganizer.Api.IntegrationTests/Seed/SeededDemoTests.cs`

**Interfaces:**
- Consumes: Task 3 `IUserAccounts`; plan 1 repositories, `LessonOrganizerDbContext`.
- Produces:
  - `sealed class DemoDataSeeder(LessonOrganizerDbContext db, IUserAccounts accounts, IStudentRepository students, ILessonPlanRepository lessonPlans, TimeProvider time)` — `Task<bool> SeedAsync(CancellationToken ct)`: returns false and writes nothing when any user exists; otherwise creates exactly the data in `plan.md` §3 "Seed data" (two users with password `Demo1234!`, five students with their levels, six lesson plans with titles, statuses and day offsets relative to today's local date from `time`), through `IUserAccounts.CreateAsync`, `Student.Create`, `LessonPlan.Create` + `ChangeStatus`, and the repositories. Throws `InvalidOperationException` naming the item when any create fails.
  - `FixedTimeProvider(DateTimeOffset now) : TimeProvider`.
  - `Seed:DemoData` (bool, default false).

- [ ] **Step 1: Write the failing tests.** Seeder (real database, `FixedTimeProvider` at 2026-11-10):

```csharp
[Fact]
public async Task Seed_EmptyDatabase_CreatesPlanMdData()
{
    Assert.True(await SeedAsync());
    await using var context = _db.NewContext();
    Assert.Equal(2, await context.Users.CountAsync(Ct));
    Assert.Equal(5, await context.Students.CountAsync(Ct));
    var plans = await context.LessonPlans.OrderBy(plan => plan.LessonDate).ToListAsync(Ct);
    Assert.Equal(new DateOnly(2026, 11, 3), plans.First().LessonDate);
    Assert.Equal([LessonPlanStatus.Delivered, LessonPlanStatus.Delivered, LessonPlanStatus.Ready, LessonPlanStatus.Ready, LessonPlanStatus.ToPrepare, LessonPlanStatus.ToPrepare],
        plans.Select(plan => plan.Status).Order());
}
```

  Also: `Seed_SecondRun_ReturnsFalseAndAddsNothing`, `Seed_DatabaseWithAnyUser_AddsNothing`, `SeededUsers_CanLogInWithDemoPassword` (`CheckPasswordAsync` → `Succeeded` for both), `Seed_CarlaHasNoPlans`. Integration (`ApiFactory` with `Seed:DemoData=true`): `Maria_SeesOnlyHerThreeStudents`, `John_SeesNoneOfMariasData`, `DeleteAna_Returns409` (workflow 3).
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Infrastructure.Tests` → build fails, `DemoDataSeeder` not found. Commit `test: demo data seed`.
- [ ] **Step 3: Implement** the seeder and the startup hook; README "Demo credentials" with the two users and what each has.
- [ ] **Step 4: Run to verify they pass.** Infrastructure and Api.IntegrationTests projects → all passed.
- [ ] **Step 5: Commit** `feat: demo data seed`.

---

### Task 9: OpenAPI and Scalar in Development

**Files:**
- Modify: `Directory.Packages.props`, `src/LessonOrganizer.Api/LessonOrganizer.Api.csproj`, `Program.cs`, `docs/stack.md` (versions; remove "optional"), `CLAUDE.md` (Commands: run API, Scalar URL, user secrets), `README.md` ("API" section), `docs/ai/AI_LOG.md`
- Test: `tests/LessonOrganizer.Api.IntegrationTests/Docs/ApiDocsTests.cs`

**Interfaces:**
- Produces: in Development only, `app.MapOpenApi().AllowAnonymous()` (`/openapi/v1.json`) and `app.MapScalarApiReference().AllowAnonymous()` (`/scalar`). Nothing mapped in other environments.

- [ ] **Step 1: Write the failing tests.** `OpenApiDocument_InDevelopment_ListsAllEndpoints` (factory with environment `Development`; `/openapi/v1.json` → 200 without a session; paths include `/api/auth/login`, `/api/students/{id}`, `/api/lesson-plans/{id}/status`), `Scalar_InDevelopment_Returns200WithoutSession`, `OpenApiAndScalar_OutsideDevelopment_Return404`.
- [ ] **Step 2: Run to verify they fail.** `dotnet test --project tests/LessonOrganizer.Api.IntegrationTests` → the three tests fail with 404 / 401. Commit `test: api docs in development`.
- [ ] **Step 3: Implement**; update `AnonymousEndpointsTests` only if it inspects endpoints beyond controller actions (it should not change).
- [ ] **Step 4: Run to verify they pass.** `dotnet test` (Docker running) → all projects passed, exit 0. `dotnet build` → 0 warnings.
- [ ] **Step 5: Manual check.** `dotnet run --project src/LessonOrganizer.Api --launch-profile https`; open `https://localhost:7141/scalar`; log in as `maria@example.com` / `Demo1234!` through `/api/auth/csrf` + `/api/auth/login` (copy the `XSRF-TOKEN` cookie value into an `X-XSRF-TOKEN` header for unsafe requests) and list students. Record the result in `AI_LOG.md`.
- [ ] **Step 6: Docs and commit.** Update `stack.md`, `CLAUDE.md`, README "API"; AI_LOG entry with red/green runs per task. Commit `feat: openapi document and scalar ui in development`.
