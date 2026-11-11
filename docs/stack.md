# Stack

Versions and tools. Backend first; the [frontend](#frontend) is at the end.

What the exercise requires for the backend: ".NET C#, ASP.NET MVC, Web API, and a database or data store". It names no version and no test framework.

## Platform

| Item | Version | Where it is set | Why |
|---|---|---|---|
| .NET SDK | 10.0.403 | `global.json`, `rollForward: latestFeature` | Installed locally. Everyone builds with the same SDK band; newer patches of 10.0.4xx are accepted. |
| Target framework | `net10.0` | `Directory.Build.props` | .NET 10 is the current LTS, supported until November 2028. .NET 11 (STS, GA expected November 2026) is not installed, brings nothing this project needs, and is too new to bet a demo on. |
| ASP.NET Core runtime | 10.0.x (10.0.14 installed) | shared framework `Microsoft.AspNetCore.App` | Controllers, cookie authentication, antiforgery, rate limiter, ProblemDetails, JSON console logging. No package needed. |
| C# | 14 | default for `net10.0` | No `LangVersion` override. |

Verified on 2026-11-10 with `dotnet --list-sdks` (6.0.428, 9.0.311, 10.0.403) and `dotnet --list-runtimes`.

### `global.json`

```json
{
  "sdk": {
    "version": "10.0.403",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

`test.runner` makes `dotnet test` use Microsoft Testing Platform (MTP). xUnit v3 4.x runs only on MTP: with the .NET 10 SDK the old VSTest path fails, and VSTest with `xunit.runner.visualstudio` finds no tests (checked on 2026-11-10).

### Package source

`nuget.config` at the root clears every machine-level source and keeps only nuget.org, so no company feed is used by this project.

### `Directory.Build.props` (every project)

| Property | Value | Why |
|---|---|---|
| `TargetFramework` | `net10.0` | One place to change the version. |
| `Nullable` | `enable` | Null bugs become compile errors. |
| `ImplicitUsings` | `enable` | Default for new projects. |
| `TreatWarningsAsErrors` | `true` | Keeps the build clean; warnings don't pile up. |
| `AnalysisLevel` | `latest-recommended` | Built-in .NET analyzers, no extra package. |
| `EnforceCodeStyleInBuild` | `true` | `.editorconfig` rules are checked by `dotnet build`. |

### Central package versions

Package versions live only in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, built into NuGet). Project files list package names without versions, so a version is changed in one place.

## Packages

Exact versions are pinned in `Directory.Packages.props` when each package is added (test packages at stage 0, EF Core, Identity and Testcontainers at stage 4, `Mvc.Testing` at stage 5), and recorded here then. Rule: ASP.NET Core and EF Core packages and `dotnet-ef` stay on 10.0.x to match the runtime; never an 11.x package.

| Package | Used by | Why |
|---|---|---|
| `xunit.v3` 4.0.1 | all test projects (via `tests/Directory.Build.props`) | Test framework; each test project is an executable on Microsoft Testing Platform, so no `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`. |
| `Microsoft.Testing.Extensions.CodeCoverage` 18.11.2 | all test projects (via `tests/Directory.Build.props`) | The exercise asks for "sufficient test coverage". `coverlet.collector` works only with VSTest. 18.0.4 crashed (`TypeLoadException`) because it targets Microsoft.Testing.Platform 1.8.4; 18.11.2 depends on MTP 2.4.0, the version xUnit v3 4.0.1 uses (looked up on nuget.org with the human's approval, 2026-11-10). Settings in `coverage.settings.xml` (project modules only, migrations excluded); `tools/coverage-summary.mjs` merges the per-project Cobertura files. |
| `Microsoft.AspNetCore.Mvc.Testing` 10.0.9 | `Api.IntegrationTests` | `WebApplicationFactory`: runs the real API in memory for HTTP tests. |
| `Microsoft.EntityFrameworkCore.SqlServer` 10.0.9 | `Infrastructure` | EF Core and its SQL Server provider: the data access layer ([ADR 0003](decisions/0003-sql-server-ef-core-docker.md)). |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.9 | `Infrastructure` | Identity's EF Core stores: users table, password hashing, lockout, security stamp. |
| `Microsoft.EntityFrameworkCore.Design` 10.0.9 (`PrivateAssets=all`) | `Infrastructure` | Lets `dotnet ef` create migrations, using a design-time `DbContext` factory in Infrastructure, so the Api is not needed as startup project. Not shipped at runtime. |
| `Testcontainers.MsSql` 4.15.0 | `Infrastructure.Tests`, `Api.IntegrationTests` | Starts a throwaway SQL Server container for the tests, so the data layer is tested against the real engine. |
| `System.Security.Cryptography.Pkcs` 10.0.13 (transitive pin) | everything that uses SQL Server | `Microsoft.Data.SqlClient` 6.1.1 (from EF Core SqlServer 10.0.9) brings 9.0.4, which has a moderate advisory (GHSA-2fmv-rv2c-59x7) and fails the build (`NU1902` as error). Pinned with `CentralPackageTransitivePinningEnabled`. |

Tool (local manifest `.config/dotnet-tools.json`, restored with `dotnet tool restore`):

| Tool | Why |
|---|---|
| `dotnet-ef` 10.0.9 | `dotnet ef migrations add`, `dotnet ef database update`. A local tool, so everyone uses the same version. |

API documentation (approved by the human on 2026-11-10; `Scalar.AspNetCore` version taken from nuget.org with their approval):

| Package | Used by | Why |
|---|---|---|
| `Microsoft.AspNetCore.OpenApi` 10.0.12 | `Api` | Generates the OpenAPI document of the endpoints (`/openapi/v1.json`, Development only). |
| `Scalar.AspNetCore` 2.20.0 | `Api`, Development only (`/scalar`) | Browser UI over that document, to show and call the API during the presentation. |

## Built-in features used instead of packages

| Need | Built-in feature |
|---|---|
| Sign-in, cookie, lockout | `SignInManager` and the Identity application cookie, in the ASP.NET Core shared framework. Infrastructure gets it with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. |
| Serving the Angular build | Static files middleware + `MapFallbackToFile("index.html")` |
| Data protection keys | `PersistKeysToFileSystem` on a Docker volume |
| CSRF | Antiforgery |
| Brute force | Rate limiter middleware |
| Errors | `AddProblemDetails` + exception handler (RFC 9457) |
| Logging | `AddJsonConsole` (structured JSON) |
| Request validation | `[ApiController]` model binding; business rules in the domain |
| Ids | `Guid.CreateVersion7()` (.NET 9+) |

## Not used

MediatR, AutoMapper, FluentValidation, EF Core InMemory or SQLite providers in tests (tests use the real SQL Server), Identity roles, mocking libraries (tests use named fakes), generic repository, nginx or another web server in front of the API. Adding a package needs a concrete reason recorded in an ADR (`docs/decisions/`).

## Docker

Two containers in `docker-compose.yml` at the repository root ([ADR 0003](decisions/0003-sql-server-ef-core-docker.md)). Image tags are pinned when the files are written (stage 4 for `db`, stage 12 for `app`) and recorded here; the human pulls them.

| Service | Image | Ports | Volumes | Notes |
|---|---|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` (pulled 2026-11-10, digest `sha256:11927f5e…`) | `127.0.0.1:1433:1433` | `mssql-data` → `/var/opt/mssql` | `ACCEPT_EULA=Y`, `MSSQL_PID=Developer`, `MSSQL_SA_PASSWORD` from `.env`. Health check with `sqlcmd` (`/opt/mssql-tools18/bin/sqlcmd -C`). |
| `app` | built from `Dockerfile` (image about 377 MB) | `127.0.0.1:8080:8080` | `dp-keys` → `/home/app/keys` (data protection keys) | Waits for `db` to be healthy. Environment: `ASPNETCORE_ENVIRONMENT=Production`, connection string to `db` with the `.env` password, `Database__MigrateOnStartup=true`, `Seed__DemoData=true`, `DataProtection__KeysPath=/home/app/keys`. Runs as the image's non-root user (`$APP_UID`). The API serves the Angular files from `wwwroot`; unknown non-API paths return `index.html`. |

`Dockerfile` (multi-stage):

| Stage | Base image | Does |
|---|---|---|
| `frontend` | `node:26-alpine` (matches `frontend/.nvmrc`) | `npm ci`, `npm run build` |
| `backend` | `mcr.microsoft.com/dotnet/sdk:10.0` | `dotnet restore`, `dotnet publish src/LessonOrganizer.Api -c Release` |
| final | `mcr.microsoft.com/dotnet/aspnet:10.0` | Published API plus the Angular `browser/` output in `wwwroot`; listens on 8080 |

Files:
- `.env.example` (committed): `MSSQL_SA_PASSWORD=` with a note on SQL Server's password rules. `.env` is git-ignored (already in `.gitignore`).
- `.dockerignore`: `bin/`, `obj/`, `node_modules/`, `.git/`, `.env`, `TestResults/`, `docs/`.

Requirements: Docker Desktop (or Docker Engine with Compose v2) and about 2 GB of free RAM for SQL Server. Verified on 2026-11-10: Docker 29.0.1 and Compose v2.40.3 installed; the engine was not running at the time.

## Commands

Filled in at stage 0, once the solution exists, and copied to `CLAUDE.md` and the README:

| Task | Command |
|---|---|
| Build | `dotnet build` |
| All tests (Docker running) | `dotnet test` (MTP; exit code 8 means zero tests ran) |
| Unit tests only, without Docker | `dotnet test --filter "FullyQualifiedName!~Infrastructure.Tests&FullyQualifiedName!~IntegrationTests"` |
| Coverage (backend) | `dotnet test --coverage --coverage-output-format cobertura --coverage-settings coverage.settings.xml`, then `node tools/coverage-summary.mjs` |
| Coverage (frontend) | `cd frontend && npx ng test --watch=false --coverage` (`@vitest/coverage-v8`; HTML in `frontend/coverage/`) |
| Restore local tools | `dotnet tool restore` |
| Add a migration | `dotnet ef migrations add <Name> --project src/LessonOrganizer.Infrastructure --output-dir Persistence/Migrations` |
| Apply migrations by hand (the app applies them at startup when `Database:MigrateOnStartup` is true) | `dotnet ef database update --project src/LessonOrganizer.Infrastructure` with `ConnectionStrings__LessonOrganizer` set |
| Database for development | `docker compose up -d db` |
| Connection string for development | `dotnet user-secrets set ConnectionStrings:LessonOrganizer "Server=127.0.0.1,1433;Database=LessonOrganizer;User Id=sa;Password=<.env value>;TrustServerCertificate=true" --project src/LessonOrganizer.Api` (use `127.0.0.1`, not `localhost`: Docker publishes the port on IPv4 only and SqlClient may try `::1` first and time out) |
| Run API | `dotnet run --project src/LessonOrganizer.Api` |
| Whole demo | `docker compose up --build` → `http://localhost:8080` |
| Reset demo data | `docker compose down -v` |
| Format | `dotnet format` |
| Run frontend (proxies `/api` to the API) | `npm start` in `frontend/` |
| Frontend tests | `npm test` in `frontend/` |
| Frontend production build | `npm run build` in `frontend/` |
| Format frontend | `npx prettier --write .` in `frontend/` |

## Frontend

What the exercise requires: "integrate the backend you just built with a frontend framework of your choice", "responsive and user-friendly", "Implement CRUD operations associated with the implemented use case", "Structured code: Organize your components and state cleanly", and, optional but desired, "no warnings in the browser console". It names no framework version.

### Platform

| Item | Version | Where it is set | Why |
|---|---|---|---|
| Angular | 22.2.5 (framework), created with CLI 22.2.6 | `frontend/package.json`, exact versions in `package-lock.json` | Current major; full framework (router, HTTP, forms) without extra libraries. |
| Angular Material / CDK | 22.2.6 | `frontend/package.json` | Added with `ng add @angular/material`. |
| Angular CLI | 22.2.6, local to the project (`npx ng`) | `frontend/package.json` devDependency | The global CLI on this machine is 21.2.13; the project uses its own local CLI so the global one doesn't matter. |
| TypeScript | 6.0.3 | `frontend/package.json` | Set by Angular, not chosen separately. `strict` on (CLI default). |
| Vitest | 5.1.0, with jsdom | `frontend/package.json` | CLI default test runner. |
| Node.js | 26.x, pinned in `frontend/.nvmrc` and `engines` | — | Installed: 26.7.0. `ng version` with Angular CLI 22.2.6 reports no unsupported-version warning. |
| npm | 11.6.3 (installed) | — | Comes with Node; lockfile committed. |

Verified on 2026-11-10 with `npx ng version` after `npx @angular/cli@22 new` (npm downloads approved by the human). File and class names follow the Angular v20+ style guide: `login-page.ts` holds `class LoginPage`, no `.component`/`.service` suffixes; models keep `.model.ts`.

### How the app is built

| Concern | Choice | Why |
|---|---|---|
| Components | Standalone (default), `OnPush`, new control flow (`@if`, `@for`) | Current Angular defaults; no NgModules. |
| Change detection | Zoneless (default for new apps) | No `zone.js`; signals drive updates. |
| State | Signals in feature services (`StudentsStore`, `LessonPlansStore`) and in components | Small app; no NgRx or other state library. Answers "organize… state cleanly" with one obvious place per feature. |
| HTTP | `HttpClient` with `withXsrfConfiguration` (`XSRF-TOKEN` cookie → `X-XSRF-TOKEN` header) and a functional interceptor that sends 401 to the login page | Matches the backend's cookie + antiforgery design (plan §4). |
| Routing | Angular Router, lazy feature routes, functional `authGuard` | Built in. |
| Forms | Typed reactive forms | Stable API; validators mirror backend limits (150, 254, 2000, 5000). The backend stays the authority. |
| Dates | `lessonDate` kept as a `yyyy-MM-dd` string end to end | Avoids time zone shifts on a date-only value. |
| Dev topology | `proxy.conf.json`: `/api` → the API's local HTTPS address | Same origin, so the auth cookie works without CORS. |
| Docker topology | `npm run build` output copied into the API's `wwwroot`; the API serves it | Same origin in the demo too; no web server of its own. |
| Tests | CLI default test runner (Vitest) | Optional per plan; used for the guard, interceptor and form validators. |
| Formatting | Prettier (config from the CLI) | Default formatter for the stack. |

### UI components

**Angular Material 22** (with Angular CDK 22), added with `ng add @angular/material` at stage 9. Material 3 theme generated from the design.md primary color (`ng generate @angular/material:theme-color`), light and dark from the system setting plus a toolbar toggle. See [ADR 0002](decisions/0002-angular-material.md) and [design.md](design.md).

Fonts and icons are bundled from npm, nothing loads from a CDN: `@fontsource-variable/inter` (Inter Variable) and `material-icons` (Material Icons Round, a 174 KB static font; the variable Material Symbols font was 5.4 MB). Added on 2026-11-10 with the human's approval.

| Need from the requirements | Material component |
|---|---|
| Lists of students | `mat-table` on wide screens, cards on phone width |
| Lesson plans main page | Board built with CDK drag-drop (`cdkDropListGroup`, `cdkDropList`, `cdkDrag`) |
| Create and edit forms, validation messages | `mat-form-field`, `mat-input`, `mat-select`, `mat-datepicker`, `mat-error` |
| Deletion confirmation | `MatDialog` |
| Success and error messages | `MatSnackBar` |
| Loading indicators | `mat-progress-bar` / `mat-progress-spinner` |
| Status change | `mat-select` or `mat-button-toggle-group` |
| Filters by student and status, search by name | `mat-select`, `mat-input` |
| Navigation and logout | `mat-toolbar`, `mat-menu` |
| Responsive layout | CDK `BreakpointObserver` + CSS grid/flex |

### Angular agent skills

When working on the frontend, AI agents consult the official Angular skills (`https://github.com/angular/skills`) for current Angular guidance. Installed once per machine with `npx skills add https://github.com/angular/skills` (the human runs it or approves it; it downloads from GitHub/npm).

### Not used

NgRx or other state libraries, NgModules, `zone.js`, CSS frameworks besides the chosen UI option, date libraries.
