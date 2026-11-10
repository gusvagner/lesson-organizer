# AI Log

Record of relevant AI interactions: what was asked, what came back, what the human changed, and how it was checked.

## 2026-11-09 — Implementation plan
- Prompt: [prompts/01-planning.md](prompts/01-planning.md)
- Output: `docs/plan.md` (understanding, ambiguities A1–A13, data model, architecture, auth, API contracts, acceptance criteria, stages 0–12).
- Issues found: the model kept a Portuguese UI from the requirements (it flagged the conflict); it proposed signing the teacher in on registration.
- Fix / decision: whole project in English; registration does not sign in; requirements intro rewritten as product description + persona. Details in the prompt file.
- Verification: none; documentation only.

## 2026-11-09 — Gap analysis against the exercise statement
- Prompt: "Explain the structure and what is missing to document before implementing, according to the exercise PDF." (asked in Portuguese)
- Output: mapping of exercise requirements to `requirements.md`/`plan.md` and a list of documentation gaps (plan approval, versions, unit tests for endpoints and data layer, coverage, GenAI section, frontend structure, ADRs, seed/config details).
- Issues found: the answer mixed gaps coming from the exercise with gaps coming from project rules, and called plan stages "steps" without saying where they came from; the human asked whether the right PDF had been read.
- Fix / decision: the model separated the two sources and listed the stages from `plan.md` §7.
- Verification: PDF and docs re-read; no commands relevant.

## 2026-11-09 — Data store: PostgreSQL → JSON files
- Prompt: "Start with JSON, no Docker for now; change the stack and validate this decision against the PDF." (asked in Portuguese)
- Output: [ADR 0001](../decisions/0001-json-file-data-store.md); `plan.md` §2 (A2, A7, A11, A12), §3 data model rewritten, §4 packages and auth, §6 data store criteria, §7 stages 0, 1, 4, 5; `requirements.md` §7; `CLAUDE.md` stack and layout.
- Issues found: the full Identity stack depends on EF Core stores; dropping it loses lockout and security stamp. The model also pointed out the primary-key requirement applies to any store ("table/object/container").
- Fix / decision: cookie authentication + Identity `PasswordHasher<T>`; rate limiting kept; lockout and session invalidation documented as not implemented. The human chose JSON over the recommended SQLite for speed of implementation.
- Verification: decision checked against the exercise wording (quoted in the ADR); documentation only, no commands run.

## 2026-11-09 — Rename the account from Teacher to User
- Prompt: "I think it's better to change teachers to users." (asked in Portuguese)
- Output: `plan.md` §2 A2, §3, §4, §5, §6, §7 and ADR 0001 updated: `users.json`, `User` entity, `IUserRepository`, `UserResponse`, `PasswordHasher<User>`.
- Issues found: a full rename would also turn `Student.TeacherId` into `UserId` and blur the business rule "a student belongs to a teacher".
- Fix / decision: "user" names the account (the exercise's term: "store users", "user creation, user login"); "teacher" stays as the owner role. `Student.teacherId` holds a `User.id`; `ICurrentTeacher` and teacher-isolation wording unchanged.
- Verification: checked against the exercise wording; `grep` for leftover `Teacher` account names in docs. No commands beyond that.

## 2026-11-09 — Seed data and .NET version
- Prompt: "Is the database fully documented? If so, move to the API; we need to document the .NET version." (asked in Portuguese)
- Output: `plan.md` §3 "Seed data" (trigger, idempotency, demo users, students and plans), §4 "Platform version", §6 and §7 stages 0 and 8; `CLAUDE.md` stack line and `global.json`.
- Issues found: the database section had no seed details although the exercise asks for "seeded data/credentials for demo purposes"; the seed acceptance criterion mentioned a `Demo` environment that was never defined.
- Fix / decision: seed only in `Development` and only on an empty store (no resurrecting deleted records); dates relative to the seed day; `Demo` environment dropped. .NET 10 LTS chosen because the exercise names no version and SDK 10.0.403 is installed.
- Verification: `dotnet --list-sdks` → 6.0.428, 9.0.311, 10.0.403; ASP.NET Core runtime 10.0.14 present.

## 2026-11-10 — Backend stack and architecture documents
- Prompt: "Let's focus on the backend stack now and document versions and architecture; we need files defining this." (asked in Portuguese)
- Output: `docs/stack.md` (platform, `global.json`, `Directory.Build.props`, central package versions, packages with versions, built-in features, commands) and `docs/architecture.md` (exercise mapping, layers diagram, allowed references, folders per project, request flow, rule placement, test projects). `plan.md` §4 reduced to a summary with links; `CLAUDE.md` source-of-truth list and layout updated.
- Issues found: the first version lookup read the internal NuGet feed and returned versions in the wrong order (e.g. `xunit.v3` 1.0.0); `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 is the newest but targets .NET 11, not the installed 10.0 runtime.
- Fix / decision: versions taken from nuget.org only, highest stable by version sort; ASP.NET Core packages kept on 10.0.x. `Result<T>` moved to Domain (the domain produces the first errors). Controller unit tests left as a proposal awaiting a decision. OpenAPI and Scalar left optional.
- Verification: `dotnet package search <id> --exact-match --source https://api.nuget.org/v3/index.json` for each package → xunit.v3 4.0.3, xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.11.0, coverlet.collector 10.1.0, Microsoft.AspNetCore.Mvc.Testing 10.0.14 (latest 10.x), Microsoft.AspNetCore.OpenApi 10.0.15, Scalar.AspNetCore 2.19.0.

## 2026-11-10 — No package repository lookups
- Prompt: "Don't look at the Stallos feed or query any package repository." (asked in Portuguese)
- Output: package versions removed from `docs/stack.md`; packages listed by name, with the rule that ASP.NET Core packages stay on 10.0.x.
- Issues found: the model had queried an internal company feed and nuget.org without being asked; the human did not want either for this project.
- Fix / decision: versions are pinned at stage 0 when packages are added; no lookups before that.
- Verification: `docs/stack.md` re-read; no commands run.

## 2026-11-10 — .NET 11 check and frontend stack
- Prompt: "Keep .NET 10 — what's new in .NET 11 and is it worth testing?" then "Define the frontend stack too: Angular 22 (latest)." (asked in Portuguese)
- Output: summary of .NET 11 from Microsoft Learn ("What's new in .NET 11"); `docs/stack.md` renamed to "Stack" with a Frontend section (platform, build choices, UI options, not used, commands).
- Issues found: the model had said .NET 11 already shipped, based on a package version; the official page says it was last updated for RC 2 with GA expected November 2026. Locally, the global Angular CLI is 21.2.13 and reports Node 26.7.0 as unsupported.
- Fix / decision: `stack.md` wording on .NET 11 corrected. Project uses a local Angular 22 CLI via `npx`; Node support for Angular 22 to be confirmed at stage 9. Angular Material vs plain CSS left for the human.
- Verification: `node --version` → v26.7.0; `npm --version` → 11.6.3; `ng version` → Angular CLI 21.2.13, Node 26.7.0 (Unsupported). No package registry queried.

## 2026-11-10 — Angular Material and Angular agent skills
- Prompt: "Also add consulting the Angular skills when needed (npx skills add https://github.com/angular/skills), and we'll use Angular Material 22." (asked in Portuguese)
- Output: [ADR 0002](../decisions/0002-angular-material.md); `stack.md` UI components (requirement → Material component) and Angular agent skills; `CLAUDE.md` rule to consult the Angular skills on frontend tasks; `plan.md` stage 9.
- Issues found: installing the skills downloads from GitHub/npm, which conflicts with the no-lookup rule unless the human approves.
- Fix / decision: the install command is documented, not run; agents ask before running it.
- Verification: documentation only; no commands run.

## 2026-11-10 — Frontend architecture
- Prompt: "Let's go to the frontend architecture; ask me and I'll answer." (asked in Portuguese)
- Output: six multiple-choice questions, then `docs/frontend-architecture.md` (exercise mapping, folders, kinds of code, routes, navigation, data flow, errors, auth, UI states, display rules, tests). `plan.md` stages 9–11 now include Angular tests; `CLAUDE.md` and `architecture.md` link the new file.
- Human answers: feature folders; top toolbar; forms on their own pages; tests for logic and main screens; inline status change; filters and search in the URL.
- Model's own calls (for review): stores `providedIn: 'root'`; logout does a full page load to clear state; antiforgery token refreshed after login/logout; lesson plan details shown on the edit page; date helper avoids `new Date('yyyy-MM-dd')` UTC shift; lists as table on desktop and cards on phones.
- Verification: documentation only; no commands run.

## 2026-11-10 — Controller unit tests and TDD in the test strategy
- Prompt: "Let's also add unit tests for the controllers. Is it documented that we'll use TDD, like in the exercise?" (asked in Portuguese)
- Output: `architecture.md` Tests: new `Api.Tests` project (controllers, `ResultHttpMapping`, `ClaimsCurrentTeacher`) and a "How tests are written (TDD)" section; exercise mapping rows for unit tests and TDD; `plan.md` stages 5–7; `CLAUDE.md` layout.
- Issues found: TDD was stated in `CLAUDE.md`, `requirements.md` §8 and `plan.md` §7, but not next to the test strategy, and nothing said how the presentation would show tests came first.
- Fix / decision: red and green committed separately when practical; failing and passing runs recorded per stage in this log; order inside a backend feature Domain → Application → Infrastructure → controller unit → HTTP integration.
- Verification: documentation only; no commands run.

## 2026-11-10 — Check docs against exercise items
- Prompt: "Are data layer, business logic layer, unit tests, the frontend key criteria, README and seeded data documented to follow the exercise?" (asked in Portuguese, with the exercise items pasted)
- Output: item-by-item check; three gaps fixed. `architecture.md`: repository contracts table (the data layer's CRUD) and integrity guards; domain checks enums with `Enum.IsDefined`. `plan.md`: "README contents" section.
- Issues found: the data layer had no list of CRUD operations; `JsonStringEnumConverter` accepts numeric enum values, so model binding alone would let `"englishLevel": 99` through to the business layer; the README had only a four-word description.
- Fix / decision: business rules stay in Application, and repositories add integrity guards inside the lock, like a foreign key or unique index; README sections defined and checked by running every command at stage 12.
- Verification: `grep` over the docs for README, enum and repository mentions; documentation only.

## 2026-11-10 — Implementation plan 1: backend core
- Prompt: "We need to plan the implementation in stages, as suggested." (asked in Portuguese)
- Output: `docs/plans/2026-11-10-1-backend-core.md` — stages 0–4 as 8 TDD tasks (scaffold, domain common, User/Student, LessonPlan, StudentService, LessonPlanService, JSON store, JSON repositories) with exact signatures, messages, test names and commands. Work split into four plans (backend core, backend API, frontend, delivery); `plan.md` §7 links them.
- Issues found while planning: `users.json` example email differed from the seed (`maria@demo.dev` vs `maria@example.com`); a root `Directory.Build.props` cannot condition package references on `IsTestProject` set later by the project, so test packages go in `tests/Directory.Build.props`; .NET 10 creates `.slnx` solutions, so the layout name changes at stage 0.
- Fix / decision: email fixed in `plan.md`; package versions come from the local NuGet cache or the human, never a feed query; human checkpoint on package source at Task 1.
- Verification: documentation only; no commands run.

## 2026-11-10 — Fifth plan: security review
- Prompt: "Where are the 4 steps documented? Add a 5th to review all security issues." (asked in Portuguese)
- Output: `plan.md` §7 lists plan 5 and stage 13; new "Security review (stage 13)" checklist (authorization, isolation, cookie, CSRF, passwords, brute force, input, errors and logs, data files, transport and headers, frontend, secrets, dependencies) with OK / Fixed / Accepted risk outcomes in `docs/security-review.md`.
- Issues found while writing the checklist: login could reveal existing emails by response time; nothing forced new endpoints to require auth; `returnUrl` after login could redirect off-site; dependency vulnerability checks query package feeds.
- Fix / decision: dummy-hash check for unknown emails and an authorization fallback policy added to the auth design (§4); `returnUrl` restricted to app paths in `frontend-architecture.md`; vulnerability checks left to the human.
- Verification: documentation only; no commands run.

## 2026-11-10 — Data store: JSON files → SQL Server with EF Core, Docker Compose
- Prompt: "What if we use SQL Server, in Docker — one Linux image with only SQL Server, .NET and Angular? Would it be better?", then "How many containers?", "Weren't we going to use EF Core?", and "Go with SQL Server and EF Core, update the docs, run with Docker as suggested." (asked in Portuguese)
- Output: [ADR 0003](../decisions/0003-sql-server-ef-core-docker.md) superseding ADR 0001; `plan.md` §2 (A2, A7, A11, A12), §3 rewritten (tables, keys, indexes, migrations, connection string, seed flag), §4 (Identity auth with lockout, running modes), §6 (database and Docker demo criteria), §7 (stages 0, 1, 4, 5, 8, 12, security checklist, README), §8; `stack.md` (EF Core, Identity, Testcontainers, `dotnet-ef`, Docker section, commands); `architecture.md` (no domain `User`, `IUserAccounts` port, Persistence and Identity folders, tests on real SQL Server); `CLAUDE.md`; `requirements.md` §7; plan 1 tasks 3, 5, 7, 8 rewritten.
- Issues found: one image with all three components would need a process supervisor and goes against one process per container; the model recommended two containers (`db`, and `app` with the API serving the Angular build), keeping one origin for the cookie and CSRF. The model had first said the plan "already" used EF Core only for the SQL Server option, which confused the human; clarified that the JSON plan had no EF Core. Other risks found while rewriting: `LIKE` treats `%`, `_`, `[` as wildcards in student search; the SPA fallback route would fall under the default-deny policy and could answer unknown `/api` paths with `index.html`; data protection keys inside the container would log everyone out on recreate; `Secure` cookies over plain HTTP in the Docker demo; lockout can be triggered by others; Docker Desktop license for large companies.
- Fix / decision: SQL Server 2022 Developer in Docker; EF Core maps domain entities with Fluent configurations (no document records, no `Restore` methods); Identity with `IdentityUserContext` (no role tables), lockout 5 attempts / 15 minutes with the same generic 401; data-layer and integration tests on Testcontainers. Escaped `LIKE` pattern with a test; SPA fallback anonymous and kept off `/api`; keys on a volume; HTTP on localhost recorded as accepted risk to verify at stage 12. Migrations and seed controlled by `Database:MigrateOnStartup` and `Seed:DemoData` instead of the environment name.
- Verification: exercise PDF re-read (pages 1–3: "a database or other data storage solution"; no product named). `docker --version` → 29.0.1; `docker compose version` → v2.40.3; `docker info` → engine not running. No package feed or image registry queried.
