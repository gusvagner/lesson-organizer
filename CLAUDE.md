# Lesson Organizer

Web app for independent English teachers to manage students and lesson plans.
ASP.NET Core Web API (Clean Architecture) + EF Core/PostgreSQL + Angular.

## Source of truth

Read these before any task. Do not contradict them; if something is unclear, ask.

- `docs/requirements.md` — what the product must do (and what is out of scope).
- `docs/plan.md` — approved design: data model, architecture, auth, API contracts, acceptance criteria, stages.

The original exercise PDF in `docs/` is local only (git-ignored). Never commit or quote it in the repo.

## Language

Everything in English: code, UI text, API, docs, commits, prompts.

## Directory layout

Items marked *(planned)* do not exist yet; create them in the stage listed in `docs/plan.md`.

```
lesson-organizer/
├── CLAUDE.md                     # this file: context and rules for AI agents
├── README.md                     # (planned) pitch, setup, run, tests, demo credentials
├── .gitignore
├── .editorconfig                 # (planned)
├── Directory.Build.props         # (planned) nullable, warnings as errors, target framework
├── docker-compose.yml            # (planned) local PostgreSQL
├── LessonOrganizer.sln           # (planned)
│
├── docs/
│   ├── requirements.md           # product requirements
│   ├── plan.md                   # approved implementation plan
│   ├── architecture.md           # (planned) layers, dependency rule, diagrams
│   ├── api.md                    # (planned) endpoints, status codes, auth
│   ├── decisions/                # (planned) ADRs: NNNN-short-title.md
│   └── ai/
│       ├── AI_LOG.md             # (planned) log of every relevant AI interaction
│       ├── prompts/              # reusable prompts, numbered in the order used
│       │   └── 01-planning.md
│       └── genai-exercise.md     # (planned) answer to the exercise's GenAI section
│
├── src/                          # (planned)
│   ├── LessonOrganizer.Domain/          # entities, enums, invariants; no dependencies
│   ├── LessonOrganizer.Application/     # use cases, contracts, repository interfaces
│   ├── LessonOrganizer.Infrastructure/  # EF Core, Identity, repositories, migrations, seed
│   └── LessonOrganizer.Api/             # controllers, auth config, composition root
│
├── tests/                        # (planned), mirrors src/
│   ├── LessonOrganizer.Domain.Tests/
│   ├── LessonOrganizer.Application.Tests/
│   ├── LessonOrganizer.Infrastructure.Tests/    # Testcontainers PostgreSQL
│   └── LessonOrganizer.Api.IntegrationTests/    # WebApplicationFactory + Testcontainers
│
└── frontend/                     # (planned) Angular app
```

Dependency rule: `Api → Infrastructure → Application → Domain`, and `Api → Application`. Domain and Application must not reference EF Core, ASP.NET Core or Identity.

## How to work

- **Plan first.** Do not write code for a stage until the plan for it is approved. Do not expand scope beyond `docs/requirements.md`.
- **Strict TDD.** Write the tests, run them and confirm they fail for the expected reason, implement, run again, refactor. Commit red and green separately when practical.
- **Never claim a test or command ran unless it actually ran.** Report real output, including failures.
- **Proportional design.** No MediatR, AutoMapper, FluentValidation, generic repositories or mocking libraries. Use hand-written, named fakes in unit tests. Adding any package needs a concrete reason recorded in an ADR.
- **Security defaults.** Teacher id comes only from the authenticated user, never from request body or route. Another teacher's resource returns 404, exactly like a missing one.
- **Contracts.** Never serialize EF entities; use the request/response records in Application.
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

Filled in during stage 0, once the solution exists.
