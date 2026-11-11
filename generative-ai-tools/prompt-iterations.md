# Prompt iterations

How the generation prompt ([prompts/01-generate.md](prompts/01-generate.md)) was built and improved before it ran. Each version records what changed, why, and who raised it. Decisions are mine (the human); the AI assistant drafted text and proposed options.

## Decisions before the first draft (2026-11-11)

| Question | Decision | Why |
|---|---|---|
| What should this section show? | That I use AI with critical judgment and validate the code | The value is the process and the decisions, not the generated code |
| Weak prompt (more errors to fix) or the prompt I really use? | The detailed prompt I use day to day, plus agents that validate the work | A deliberately weak prompt would be artificial; a good prompt still leaves subtle mistakes worth catching |
| Same repository or a new one? | Same repository, isolated folder `generative-ai-tools/` | One place for the panel; git history shows the raw output and each correction |
| Where do the documents live? | Everything inside `generative-ai-tools/`, not in `docs/` | The answer stays self-contained |
| Stack | Same as the Lesson Organizer: .NET 10, ASP.NET Core, EF Core, SQL Server, xUnit v3 | The role is .NET; corrections in a stack I know are more convincing |
| Authentication | JWT bearer, user id from `sub`; tests mint their own tokens | The exercise assumes a User model exists; JWT fits an API for any client. (The Lesson Organizer uses a cookie session because its client is a same-origin SPA.) |
| Shape | Lean: one API project + one test project | Proportional to CRUD on one table; keeps the AI's mistakes visible |
| Scope | CRUD, ownership, filter by status, paging, ProblemDetails, migrations, unit + integration tests on real SQL Server | What the exercise asks, with the edge cases that matter |
| Out of scope | Frontend, Docker demo, login/registration, demo data | Not asked |
| Database | Its own database (`TaskManagement`); tests start their own SQL Server container | Nothing shared with the Lesson Organizer |

## v1 — first draft

Stack and exact versions (and "do not query package registries"), projects, data model, JWT rules with 404 for other users' tasks, endpoints table, tests to write first, "done" = build with no warnings and tests green, and "report only commands you actually ran".

## v2 — architecture and parts

- **Raised by me:** the prompt lacked architecture detail, and the work should be split into parts that are done and validated one at a time.
- **Changes:**
  - Folder tree with the role of every file; dependencies only inward (controller → service → repository); rules per kind of code (thin controller, service returns a result instead of throwing, repository always takes the owner id, DTOs only, `TimeProvider`, options validated at startup).
  - Named the status enum `TaskItemStatus`, because `TaskStatus` collides with `System.Threading.Tasks.TaskStatus` — a mistake the AI would likely make.
  - Five parts (skeleton → tasks core → persistence → HTTP and auth → final check). After each part the agent stops; I validate independently before it continues.
  - Test tokens minted with `JsonWebTokenHandler` from the existing dependency, so no extra package.

## v3 — strict TDD

- **Raised by me:** "is it saying we will use TDD?" — v2 only said "write tests first".
- **Why it mattered:** without running the tests red first, nobody knows whether they can catch anything; the exercise prefers TDD.
- **Change:** a "Strict TDD in every part" section — write tests, run them and confirm they fail for the expected reason, paste the red output in `generation-log.md`, implement the minimum, paste the green output, refactor. A test that passes before its code exists must be reported, not hidden. My validation of each part checks that the red run really happened.

## v4 — RESTful and CRUD made explicit

- **Raised by me:** "where does it say RESTful? where does it talk about CRUD?"
- **Finding:** "RESTful" was one word in the first sentence and CRUD was only implied by the endpoints table — nothing stopped `POST /api/tasks/{id}/complete` or `GET /api/getTasks`.
- **Change:** a "Requirements" section: REST rules (plural resource, no verbs in URLs, standard method meanings, status codes, `Location` on create, idempotent PUT), full CRUD spelled out, the four task fields, and ownership by user.
- **Decision:** the exercise writes `due_date`; the API uses `dueDate` so every JSON field is camelCase.

## Process decisions

- I commit each part's raw output myself on the branch `feat/genai-exercise`; the AI assistant does not commit.
- Before the run, the folder was isolated from the Lesson Organizer with its own `global.json` (SDK 10.0.403, Microsoft Testing Platform), `nuget.config` (nuget.org only), `Directory.Build.props` (net10.0, nullable, implicit usings, warnings as errors) and an empty `Directory.Packages.props` (central versions on). Without them the generated projects would inherit the main project's analyzers and package versions, and a failure caused by that would look like the AI's mistake. The folder is also excluded from the Lesson Organizer's Docker build context.
- The prompt runs in a fresh Claude Code agent told to work only inside this folder; it sees the prompt, not the Lesson Organizer's code or this conversation.
