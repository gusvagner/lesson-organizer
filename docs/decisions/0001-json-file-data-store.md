# 0001 — JSON file data store instead of a database

- Status: superseded by [0003 — SQL Server with EF Core, run with Docker Compose](0003-sql-server-ef-core-docker.md)
- Date: 2026-11-09

## Context

The first plan used EF Core with PostgreSQL in Docker, with Testcontainers for tests. The goal for now is to get to a running demo quickly and without Docker.

The exercise allows this. It asks for "a database or data store" and for "a database or other data storage solution with at least one table/object/container to store data for the application and an additional one to store users", each with "a unique identifier (primary key) and at least two other fields". It also asks for a data access layer with CRUD operations, unit tests for it, and that "the user information is stored in the data".

## Decision

Store data as JSON files, one per collection: `users.json`, `students.json`, `lesson-plans.json`. Details are in [plan.md §3](../plan.md#3-data-model).

- Repositories in Infrastructure implement `IUserRepository`, `IStudentRepository`, `ILessonPlanRepository` from Application.
- One process-wide lock serializes access; writes go to a temp file and replace the original atomically.
- Foreign keys and email uniqueness are enforced by the application, inside the lock.
- Authentication uses ASP.NET Core cookie authentication with Identity's `PasswordHasher<T>`, not the full Identity stack, whose stores assume EF Core.

How the exercise requirements are met:

| Requirement | How |
|---|---|
| Container for app data + one for users | `students.json`, `lesson-plans.json` + `users.json` |
| Unique identifier + 2 fields | `id` (Guid v7) + `name`, `email`, … |
| Data access layer with CRUD | JSON repositories |
| User information stored | `users.json`, with a password hash |
| Unit tests for the data layer | Repository tests against a per-test temp directory |

## Consequences

Good:
- `dotnet run` is the whole setup. No database server, no Docker, no connection string.
- Tests are fast and independent (each one gets its own directory).
- No extra packages.
- Shows the storage is a detail: replacing it with EF Core changes only Infrastructure and its tests.

Bad, accepted for this project:
- Single API instance only; the lock is in-process.
- Every operation loads the whole collection. Fine for one teacher's data; not for large volumes.
- No referential integrity, uniqueness or transactions from an engine; the application has to get them right, and tests cover them.
- No migrations; a breaking format change means deleting `data/` (dev/demo only).
- No account lockout or security stamp, which the full Identity stack would have given.

## Alternatives considered

- **PostgreSQL + EF Core (Docker):** the original plan. Most production-like; needs Docker for the app and the tests.
- **SQLite + EF Core:** no Docker, real SQL and full Identity. A good next step if the JSON store becomes a limit.
- **EF Core InMemory provider:** not persistent and ignores foreign keys. Rejected: user data must be stored.
- **MongoDB / LiteDB:** no first-party Identity support; adds a package and a server or an engine.
