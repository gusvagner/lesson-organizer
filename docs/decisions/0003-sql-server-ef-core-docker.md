# 0003 — SQL Server with EF Core, run with Docker Compose

- Status: accepted
- Date: 2026-11-10
- Supersedes: [0001 — JSON file data store](0001-json-file-data-store.md)

## Context

ADR 0001 chose JSON files to reach a running demo quickly. Before any code was written, the human chose a real database instead, run with Docker.

The exercise allows either. It asks for "a database or data store" and for "a database or other data storage solution with at least one table/object/container to store data for the application and an additional one to store users", each with "a unique identifier (primary key) and at least two other fields". The panel also evaluates "the main backend and frontend best practices" and asks the candidate to explain coding decisions in a code review.

What the JSON store cost, seen while planning: a hand-written lock, atomic file replacement, corrupt-file handling, and foreign keys and email uniqueness enforced only by application code. All of that is code to write, test and defend in the review, and none of it is the product. It also ruled out the full Identity stack, so account lockout and session invalidation were left out.

## Decision

- **Database:** SQL Server 2022, Developer edition, from the official image `mcr.microsoft.com/mssql/server` (tag pinned at stage 0).
- **Data access:** EF Core with the SQL Server provider, in Infrastructure only. One `DbContext`; the schema is created by EF Core migrations kept in the repository.
- **Mapping:** EF Core maps the domain entities `Student` and `LessonPlan` through Fluent API configuration classes in Infrastructure. Domain has no EF attributes and no EF reference.
- **Users and login:** ASP.NET Core Identity with its EF Core stores. `ApplicationUser : IdentityUser<Guid>` with a `Name` lives in Infrastructure; the Identity tables are the users table the exercise asks for. Application talks to it through its own port (`IUserAccounts`), never through `UserManager`.
- **Repositories:** the same interfaces as before (`IStudentRepository`, `ILessonPlanRepository`), implemented with the `DbContext`. Application and Domain do not change.
- **Integrity in the database:** primary keys, foreign keys with `ON DELETE NO ACTION`, a unique index on the normalized email, transactions per `SaveChanges`.
- **Docker Compose, two containers:**
  - `db`: SQL Server.
  - `app`: one image built from a multi-stage `Dockerfile` (Node builds Angular, the .NET SDK publishes the API, the final image has only the ASP.NET Core runtime). The API serves the built Angular files, so the browser sees one origin and the auth cookie and CSRF work without CORS.
- **Day-to-day development:** only `db` runs in Docker; the API runs with `dotnet run` and Angular with `npm start` (hot reload, `/api` proxied).
- **Tests of the data layer:** real SQL Server started by Testcontainers, each test on its own database created by the migrations. Docker must be running for `Infrastructure.Tests` and `Api.IntegrationTests`.

How the exercise requirements are met:

| Requirement | How |
|---|---|
| Table for app data + one for users | `Students`, `LessonPlans` + Identity's `AspNetUsers` |
| Unique identifier + 2 fields | `Id` (`uniqueidentifier`, Guid v7) + `Name`, `Email`, … |
| Data access layer with CRUD | EF Core repositories behind Application interfaces |
| User information stored | `AspNetUsers`, with a password hash |
| Unit tests for the data layer | Repository tests against a real SQL Server (Testcontainers) |
| Seeded data/credentials | Demo seed applied at startup on an empty database |

## Consequences

Good:
- Integrity comes from the engine (keys, unique index, transactions), not from hand-written locks.
- Less custom code to defend; EF Core and Identity are what a .NET panel expects to see.
- Account lockout and session invalidation through the security stamp come with Identity.
- `docker compose up` runs the whole demo with seeded data; the reviewer installs only Docker.

Bad, accepted:
- Docker is required for the demo and for two test projects. Data-layer tests are slower than temp-file tests.
- More packages: EF Core SQL Server, EF Core Design, Identity EF Core, Testcontainers, and the `dotnet-ef` tool.
- SQL Server needs about 2 GB of RAM.
- Docker Desktop is free only for personal use, education and companies under 250 employees and USD 10 million revenue; a machine owned by a larger company needs its license (or Docker Engine without Desktop).
- Domain entities need a constructor EF Core can use (private, no business rules); the domain stays free of EF types.

## Alternatives considered

- **JSON files (ADR 0001):** no Docker and no packages, but integrity and concurrency by hand and no lockout. Replaced by this ADR.
- **One image with SQL Server, .NET and Angular together:** rejected. One container should run one process; it would need a process supervisor, a very large image, and a rebuild of everything for any change.
- **Three containers (`db`, `api`, nginx `web`):** works, but adds a proxy to configure and explain with no gain for this app; the API can serve the static files.
- **SQLite + EF Core:** no Docker, but less like what the panel uses day to day.
- **PostgreSQL + EF Core:** equally valid; SQL Server is the usual pair for .NET and has first-party tooling.
