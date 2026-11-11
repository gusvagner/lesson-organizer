# Lesson Organizer

Web app for independent English teachers to manage students and lesson plans.
.NET 10 (LTS) ASP.NET Core Web API (Clean Architecture) + SQL Server with EF Core and ASP.NET Core Identity + Angular 22. Runs with Docker Compose (`db` + `app`).

## Source of truth

Read these before any task. Do not contradict them; if something is unclear, ask.

- `docs/requirements.md` — what the product must do (and what is out of scope).
- `docs/plan.md` — approved design: data model, auth, API contracts, acceptance criteria, stages.
- `docs/architecture.md` — backend: layers, dependency rule, folders per project, where each rule lives, test projects.
- `docs/frontend-architecture.md` — Angular: folders, kinds of code, routes, data flow, auth, UI states, tests.
- `docs/stack.md` — backend and frontend versions (.NET 10, Angular 22), build settings, packages, Docker, commands.
- `docs/design.md` — the frontend look: color tokens (light and dark), type, shape, components.
- `docs/decisions/` — ADRs.

The original exercise PDF in `docs/` is local only (git-ignored). Never commit or quote it in the repo.

## Language

Everything in English: code, UI text, API, docs, commits, prompts.

## Directory layout

Items marked *(planned)* do not exist yet; create them in the stage listed in `docs/plan.md`.

```
lesson-organizer/
├── CLAUDE.md                     # this file: context and rules for AI agents
├── README.md                     # pitch, setup, run, tests, demo credentials
├── .gitignore
├── .editorconfig                 # code style checked by dotnet build
├── global.json                   # pins .NET SDK 10.0.403
├── Directory.Build.props         # nullable, warnings as errors, net10.0
├── Directory.Packages.props      # central package versions
├── nuget.config                  # nuget.org only, no company feeds
├── LessonOrganizer.slnx          # solution
├── .config/dotnet-tools.json     # local tools: dotnet-ef
├── docker-compose.yml            # db (SQL Server) + app (API serving Angular)
├── Dockerfile                    # multi-stage: Angular build, .NET publish, ASP.NET runtime
├── .dockerignore
├── coverage.settings.xml         # backend coverage: project modules only, migrations excluded
├── tools/coverage-summary.mjs    # merges the per-test-project coverage reports
├── .env.example                  # placeholder for the SQL Server password; .env is git-ignored
│
├── docs/
│   ├── requirements.md           # product requirements
│   ├── plan.md                   # approved implementation plan
│   ├── architecture.md           # backend layers, dependency rule, diagrams
│   ├── frontend-architecture.md  # Angular structure, routes, state, tests
│   ├── design.md                 # design system: colors, type, shape, components (frontend look)
│   ├── stack.md                  # versions, build settings, packages, commands
│   ├── api.md                    # (planned) endpoints, status codes, auth
│   ├── decisions/                # ADRs: NNNN-short-title.md
│   ├── plans/                    # task-by-task implementation plans: YYYY-MM-DD-N-title.md
│   └── ai/
│       ├── AI_LOG.md             # log of every relevant AI interaction
│       ├── prompts/              # reusable prompts, numbered in the order used
│       │   └── 01-planning.md
│       └── genai-exercise.md     # (planned, plan 7) answer to the exercise's GenAI section
│
├── src/                          #
│   ├── LessonOrganizer.Domain/          # entities, enums, invariants; no dependencies
│   ├── LessonOrganizer.Application/     # use cases, contracts, repository interfaces
│   ├── LessonOrganizer.Infrastructure/  # EF Core DbContext, migrations, repositories, Identity, seed
│   └── LessonOrganizer.Api/             # controllers, auth config, composition root
│
├── tests/                        # mirrors src/
│   ├── LessonOrganizer.Domain.Tests/
│   ├── LessonOrganizer.Application.Tests/
│   ├── LessonOrganizer.Infrastructure.Tests/    # repositories against SQL Server (Testcontainers)
│   ├── LessonOrganizer.Api.Tests/               # controller unit tests with named fakes
│   └── LessonOrganizer.Api.IntegrationTests/    # WebApplicationFactory + SQL Server (Testcontainers)
│
└── frontend/                     # Angular 22 app (Material); core/, shared/, features/ as in docs/frontend-architecture.md
```

Dependency rule: `Api → Infrastructure → Application → Domain`, and `Api → Application`. Domain and Application must not reference ASP.NET Core, Identity or EF Core.

## How to work

- **Plan first.** Do not write code for a stage until the plan for it is approved. Do not expand scope beyond `docs/requirements.md`.
- **Strict TDD.** Write the tests, run them and confirm they fail for the expected reason, implement, run again, refactor. Commit red and green separately when practical.
- **Never claim a test or command ran unless it actually ran.** Report real output, including failures.
- **Proportional design.** No MediatR, AutoMapper, FluentValidation, generic repositories or mocking libraries. Use hand-written, named fakes in unit tests. Adding any package needs a concrete reason recorded in an ADR.
- **Security defaults.** Teacher id comes only from the authenticated user, never from request body or route. Another teacher's resource returns 404, exactly like a missing one.
- **Contracts.** Never serialize domain entities to the API; it uses the request/response records in Application. EF Core maps the entities only through Fluent configuration classes in Infrastructure; Domain has no EF attributes or references.
- **Secrets.** No connection string or password in committed files. Development uses `dotnet user-secrets`; Docker uses `.env` (git-ignored), with placeholders in `.env.example`.
- **Angular skills.** For any frontend task (components, signals, forms, routing, HTTP, testing, Angular Material), consult the official Angular agent skills from `https://github.com/angular/skills` before writing code. Install once with `npx skills add https://github.com/angular/skills` (ask the human before running it).
- Small commits, Conventional Commits style (`feat:`, `test:`, `docs:`, `refactor:`, `chore:`).

## AI log

After each relevant AI interaction, add an entry to `docs/ai/AI_LOG.md`:

```
## YYYY-MM-DD — <short title>
- Prompt: <link to docs/ai/prompts/NN-*.md or the prompt itself>
- Output: <what the AI produced, files/commits>
- Issues found: <what was wrong, missing or risky>
- Fix / decision: <what the human changed and why>
- Verification: <commands actually run and their result>
```

Reusable prompts go in `docs/ai/prompts/NN-short-title.md`.

## Commands

| Task | Command |
|---|---|
| Build | `dotnet build` |
| All tests | `dotnet test` (Microsoft Testing Platform, set in `global.json`) |
| One test project | `dotnet test --project tests/LessonOrganizer.Domain.Tests` |
| Run API | `dotnet run --project src/LessonOrganizer.Api` |
| Format | `dotnet format` |
| Restore local tools | `dotnet tool restore` |
| Add a migration | `dotnet ef migrations add <Name> --project src/LessonOrganizer.Infrastructure --output-dir Persistence/Migrations` |
| Install frontend packages | `cd frontend && npm ci` |
| Frontend tests | `cd frontend && npx ng test --watch=false` |
| Frontend build | `cd frontend && npx ng build` |
| Frontend format | `cd frontend && npx prettier --write src` |
| Run frontend (needs the API on https://localhost:7141) | `cd frontend && npm start` → `http://localhost:4200`, `/api` proxied to the API |
| Coverage (backend) | `dotnet test --coverage --coverage-output-format cobertura --coverage-settings coverage.settings.xml` then `node tools/coverage-summary.mjs` |
| Coverage (frontend) | `cd frontend && npx ng test --watch=false --coverage` |
| Docker demo | `docker compose up --build` → `http://localhost:8080` |
| Database for development | `docker compose up -d db` (copy `.env.example` to `.env` first) |
| Connection string for development | `dotnet user-secrets set ConnectionStrings:LessonOrganizer "Server=127.0.0.1,1433;Database=LessonOrganizer;User Id=sa;Password=<.env value>;TrustServerCertificate=true" --project src/LessonOrganizer.Api` |
| Run API (applies migrations and seeds demo data in Development) | `dotnet run --project src/LessonOrganizer.Api --launch-profile https` → `https://localhost:7141`, API docs at `https://localhost:7141/scalar` |

Tests in `Infrastructure.Tests` start their own SQL Server container: Docker must be running.
