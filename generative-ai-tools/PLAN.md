# GenAI exercise — plan

The exercise's "Generative AI tools" section asks for the prompt I would use to generate a RESTful task-management API, the output code (or a representative sample), and how I validated the AI's suggestions, corrected the output, and handled edge cases, authentication and validation. This folder is that answer, done for real instead of described.

## What it should show

That AI is used with critical judgment: every line the AI produced is checked, by tests and by an independent AI reviewer, and every finding gets a human decision — fix, decline, or accept as a risk — including findings where the reviewer itself is wrong.

## How it is done (the same way I work day to day)

1. **Generation prompt** ([prompts/01-generate.md](prompts/01-generate.md)): a detailed prompt, the kind I actually write — stack and versions, rules, validation, authentication, tests, and what "done" means. Iterated with care *before* it runs; every change and its reason is in [prompt-iterations.md](prompt-iterations.md).
2. **One real run** in a fresh agent (Claude Code subagent) that has no access to the Lesson Organizer context beyond this folder. The output is committed **raw**, exactly as produced, before anyone touches it. The prompt is not re-run until the output looks good: that would hide what the AI gets wrong.
3. **Validation by tools**: build, tests, and the commands the agent claims it ran, run again by me.
4. **Review agent in every part** (added in prompt v5): after each part, a fresh reviewer subagent reads the part against the prompt; the last one reviews the whole project. Its report goes into `generation-log.md` as it came, and the generating agent decides each finding — fix (test first), decline (with the reason) or defer. This replaced the separate review prompt planned at first.
5. **Human validation and triage**: I validate every part and every decision before the next part starts ([validation-log.md](validation-log.md)); the findings left open at the end are decided by the human. The reviewer's mistakes are recorded too.
6. **Answer** ([README.md](README.md)): the prompts, a sample of the generated code, the table "what the AI got wrong → how it was caught → how it was fixed", and how edge cases, authentication and validation ended up.

## Decisions (made with the human on 2026-11-11)

| Topic | Decision | Why |
|---|---|---|
| Where | Folder `generative-ai-tools/` in the Lesson Organizer repository, isolated (own `global.json`, `nuget.config`, `Directory.Build.props`, `Directory.Packages.props`, solution) | The panel sees everything in one place; git history shows the raw output and each correction |
| Stack | .NET 10, ASP.NET Core, EF Core with SQL Server, xUnit v3 — same as the Lesson Organizer | The role is .NET; corrections are more convincing in a stack I know well |
| Auth | JWT bearer; the API only validates tokens issued elsewhere and reads the user id from `sub`; tests mint their own tokens | The exercise says to assume a User model exists; JWT fits an API used by any client. The Lesson Organizer uses a cookie session because its client is a same-origin SPA — each choice has its reason |
| Shape | Lean: one API project (endpoints, a service with the rules, EF Core data access) + one test project | Proportional to CRUD on one table; makes the AI's mistakes easy to see |
| Scope | Task CRUD; title, description, status, due date; tasks owned by the caller; filter by status and paging; ProblemDetails errors; migrations; unit tests and integration tests on a real SQL Server (Testcontainers) | What the exercise asks, with the edge cases that matter |
| Out of scope | Frontend, Docker demo, login/registration endpoints, demo data | Not asked; would turn the answer into a second project |
| Database | A separate database (`TaskManagement`); tests start their own SQL Server container | No tables shared with the Lesson Organizer |
| Done | `dotnet build` with no warnings and `dotnet test` green, run by me | Same bar as the main project |

## Phases

| Phase | What | Status |
|---|---|---|
| 1 | Iterate the generation prompt with the human; isolate the folder; run it in a fresh agent **part by part** (skeleton → tasks core → persistence → HTTP and auth → final check). Each part is strict TDD with the failing (red) and passing (green) runs pasted in `generation-log.md`. After each part the agent stops; I validate independently (re-run build and tests, check that the red run really happened and failed for the right reason, read the code against the prompt, note findings in `validation-log.md`); the part's output is committed on the branch, then I tell the agent to continue | done |
| 2 | ~~Write a separate review prompt and run the review agent~~ — replaced by a reviewer subagent inside every part (prompt v5), with a review of the whole project in part 5 | done in phase 1 |
| 3 | Triage the findings still open with the human | done — see the end of [validation-log.md](validation-log.md) |
| 4 | Write `README.md` (the answer) and link it from the main README's "AI usage" | done |
