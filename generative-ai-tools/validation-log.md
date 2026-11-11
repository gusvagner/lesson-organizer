# Validation log

My independent check of each part the AI produced, done before the part is committed and before the agent continues. I re-run the commands myself instead of trusting the agent's report, check the red run in `generation-log.md`, and read the code against [the prompt](prompts/01-generate.md).

## Part 1 — Skeleton (2026-11-11)

**Re-run by me** (from `generative-ai-tools/`): `dotnet build` → 0 warnings, 0 errors; `dotnet test` → 13 total, 0 failed. Matches the agent's report.

**TDD evidence** (`generation-log.md`): real and for the right reasons.
- Red 1: the tests did not compile (CS0234/CS0246: `TaskManagement.Api`, `JwtOptions`, `JwtOptionsValidator` missing).
- Red 2: with empty stubs (validator always "valid", nothing registered) 10 of 13 failed on wrong results — this is what proves the assertions can catch a bug.
- The agent reported, without being asked twice, that 3 tests passed before their code existed (the smoke test and the two "valid key is accepted" tests) and that the blank-issuer tests failed with an unclear `ArgumentNullException` message.

| # | Finding | Verdict |
|---|---|---|
| 1 | `Program.cs` only wires services and middleware; JWT options bound from `Jwt` and validated at startup (`ValidateOnStart`) | OK, as the prompt asks |
| 2 | Signing key length counted in UTF-8 bytes, with a test using 2-byte characters (16 chars = 32 bytes); error messages give the byte count, never the key | OK — a good edge case the prompt did not spell out |
| 3 | No signing key in committed files (`appsettings.json` has issuer and audience only; development uses user secrets, tests set their own) | OK |
| 4 | The test project uses `xunit.v3.mtp-v2` 4.0.1 instead of the `xunit.v3` package the prompt implies | Accepted — same xUnit v3 version, the explicit Microsoft Testing Platform v2 variant; mentioned in the agent's report |
| 5 | The smoke test asserts a 404 for a missing endpoint: it proves the host starts but passes with no code at all | Weak test, flagged by the agent itself; acceptable for a skeleton |
| 6 | `UserSecretsId` is `task-management-api`, not a GUID | Minor, harmless |
| 7 | `AddJwtAuthentication` not written yet | OK — the prompt puts the JWT setup in part 4 |
| 8 | `dotnet format` fails: the parent repository's `.editorconfig` asks for an `Async` suffix on test methods | **Environment gap, mine, not the AI's:** the isolation files missed an `.editorconfig` with `root = true`. Build and tests are not affected. Fix after part 1 is committed, as a separate environment change |

**Result (before the agent review):** part 1 accepted and committed by the human (`genai - part 1`). Before part 2: added `generative-ai-tools/.editorconfig` with `root = true`; `dotnet format --verify-no-changes` → passes (environment fix, recorded in `prompt-iterations.md`). The prompt moved to v5 (independent review agent in every part), applied first to part 1.

## Part 1 — independent agent review (2026-11-11, prompt v5)

The generating agent dispatched a fresh reviewer subagent, pasted its report as it came, decided each finding, and stopped before part 2 as asked.

**Re-run by me:** `dotnet build` → 0 warnings, 0 errors; `dotnet format --verify-no-changes` → exit 0; `dotnet test` → 15 total, 0 failed. Matches the report. No part 2 file exists.

**The reviewer:** 0 Critical, 0 Important, 6 Minor. It caught a real gap that my own validation missed: a signing key of 32 or more whitespace characters passed validation.

| # | Reviewer finding | Agent's decision | My verdict |
|---|---|---|---|
| 1 | The log still said `dotnet format` fails; the `.editorconfig` fix was not logged | Addendum in the log, original entry kept; noted that `root = true` leaves format checking whitespace only (no C# naming rules) | Agree. The narrower format check is my choice for isolation; the prompt sets no naming rules |
| 2 | The red 1 excerpt was trimmed and a CS0246 on line 26 was missing | **Partly declined, with evidence:** the saved raw output has exactly the four errors logged; the compiler does not repeat CS0246 after a failed `using`, so the reviewer guessed. Admitted the real part: duplicate MSBuild lines and the path prefix were cut without saying so | Agree — the best decision of the review: it checked the claim against the raw output instead of accepting it, and owned the part that was right |
| 3 | A whitespace-only signing key passed | Fixed test-first (red, then the validator rejects it) | Agree; checked the diff: a key of ≥ 32 whitespace bytes is now rejected |
| 4 | Registering the JWT options twice registered the validator twice | Fixed now instead of in part 4 (`TryAddEnumerable`), test-first | Agree; one line, and part 4 will call it again |
| 5 | Two tests could fail with an unclear message | Fixed in the tests only (assert the failure before its message); no red run, and the log says so | Agree |
| 6 | `UserSecretsId` was not a GUID | Fixed (`3f6c2d8e-…`) | Agree |

**Minor note of mine:** the reviewer's report was pasted with its own `##` headings, so it breaks the log's heading structure (its sections look like top-level parts). Cosmetic; left as it came, since "as it came" is the rule.

**Result:** part 1 with its review accepted. Ready for the human to commit, then part 2.

## Part 2 — Tasks core (2026-11-11, prompt v5)

**Re-run by me:** `dotnet build` → 0 warnings, 0 errors; `dotnet format --verify-no-changes` → exit 0; `dotnet test` → 59 total, 0 failed. Matches the report. No part 3 file exists.

**TDD evidence:** red 1 = 17 compile errors (missing types); red 2 = real types with stub behaviour, 29 of 56 failed for the expected reasons. The agent separated a setup error (its first stub broke the build with CS9113) from a test result, and reported the 12 tests that passed against the stubs, explaining why the 4 not-found ones prove nothing on their own. When a review fix's test passed at once because the code already saved, it removed the save call to see the test fail, then restored it — a mutation check it was not asked for.

**Code read by me against the prompt:**

| # | Point | Verdict |
|---|---|---|
| 1 | The owner comes only from `ICurrentUser`; `CreateTaskRequest` has no user field, so nothing can be created for another user | OK |
| 2 | Title trimmed before the length check (200 chars + spaces passes); blank description → null; new tasks `Todo`; `UpdatedAt` moves on update, `CreatedAt` does not; times from `TimeProvider` in UTC | OK |
| 3 | The service returns a closed result type (success / validation errors by camelCase field / not found) and never throws for expected outcomes | OK |
| 4 | Validation messages give the limit and the value received, never echo the title text | OK |
| 5 | `ITaskRepository.AddAsync` takes no owner id (the entity carries it), unlike "every method takes the owner id" | **Accepted deviation:** the service sets the owner from the token before calling it, so it cannot widen access; flagged by the agent itself |
| 6 | An invalid PUT body on another user's task returns 400, not 404 | **Accepted:** a missing task with the same body also gets 400, so nothing about the other user's task leaks |

**The reviewer:** 0 Critical, 2 Important, 5 Minor.

| # | Reviewer finding | Agent's decision | My verdict |
|---|---|---|---|
| I1 | A huge `page` overflows the offset (a 500 on SQL Server in part 3) | Fixed test-first, with a test at the exact limit | Agree |
| I2 | A token with a valid Guid `sub` but no `Users` row would hit the FK → 500 | Deferred to part 4: check the user exists when the token is validated → 401, with a test | Agree to defer; **I will check it in part 4** (it costs one query per request — acceptable for this API) |
| M | Update not proven to save | Fixed (fake records saves) + mutation check | Agree |
| M | Default-status test cannot tell `Todo` from "never set" (enum zero value) | Declined: no unit test can catch it; string-status checks in parts 3 and 4 do | Agree — a correct decline |
| M | The log's reason for version-7 Guids was wrong for SQL Server | Fixed: `Guid.NewGuid()`, log corrected, primary-key indexing left to part 3 | Agree |
| M | Two counts off by one in the log; `FixedClock` could advance | Fixed (counts; renamed `ManualClock`) | Agree |

**Result:** part 2 accepted. To carry into part 4: the `sub`-without-user case (I2).

## Part 3 — Persistence (2026-11-11, prompt v5)

**Re-run by me:** `dotnet build` → 0 warnings, 0 errors; `dotnet format --verify-no-changes` → exit 0; `dotnet test` → 87 total, 0 failed (one SQL Server container from the local image). Matches the report. No part 4 file exists.

**TDD evidence:** three reds, each for its reason — red 1 missing types (6 compile errors); red 2 no schema yet (21 × "Invalid object name 'Users'", the FK test got error 208 instead of 547); red 3 schema in place but repository stubbed (10 wrong results). Constraint tests assert SQL Server error numbers (2628, 547, 515, 2601), not just "an exception", because a missing table also throws — a detail that keeps the tests honest. The BOM that `dotnet ef` writes was caught by `dotnet format` and removed.

**Schema read by me in `InitialCreate`:**

| Table | What it has | Verdict |
|---|---|---|
| `Users` | `Id` uniqueidentifier PK; `Email` nvarchar(320) NOT NULL with a unique index; `Name` nvarchar(200) | OK — 320 is the longest valid email address; uniqueness ignores case under the default collation |
| `Tasks` | `Id` PK **nonclustered**; `UserId` FK → `Users` (cascade); `Title` nvarchar(200) NOT NULL + `CK_Tasks_Title_NotBlank`; `Description` nvarchar(2000) NULL; `Status` nvarchar(20) (text); `DueDate` date NULL; `CreatedAt`/`UpdatedAt` datetimeoffset; clustered index `(UserId, CreatedAt)` | OK — matches the prompt; the check constraint and the clustering go beyond it |

| # | Point | Verdict |
|---|---|---|
| 1 | Clustered on `(UserId, CreatedAt)`, random Guid PK nonclustered — every query is per user, so a user's rows sit together and inserts do not scatter; the clustered key also serves the FK | Agree; a sound answer to the question I handed over, with the rejected options in the log |
| 2 | Every repository query goes through `OwnedBy(ownerId)`; delete is `ExecuteDelete` scoped to owner and id; `UpdateAsync` refuses a task of another owner (a programming error → exception) | OK |
| 3 | List order: no-date last, then due date, creation time, id — deterministic | OK |
| 4 | `System.Security.Cryptography.Pkcs` pinned to 10.0.13 after NU1902/NU1903 on the transitive 9.0.4 | Agree — the same vulnerable transitive package and the same pin as the Lesson Organizer; not a new dependency |
| 5 | Cascade delete from `Users` to `Tasks` — not asked for | Accepted: there is no user endpoint; noted as an unprompted choice for the answer |
| 6 | `dotnet-tools.json` at the folder root (the .NET 10 default), not `.config/` | OK |
| 7 | Every test, unit tests included, needs Docker (one shared container) | Accepted trade-off of "one container per run" in the prompt |

**The reviewer:** 0 Critical, 0 Important, 4 Minor. It agreed with the clustering and the pin.

| # | Reviewer finding | Agent's decision | My verdict |
|---|---|---|---|
| 1 | The database enforced only the maximum title length | Fixed test-first: `CK_Tasks_Title_NotBlank`, tested by error number **and** constraint name (547 is also the FK error); `InitialCreate` regenerated since it was not committed; gap noted (tabs/line breaks pass in SQL, the validator rejects them) | Agree, including regenerating instead of stacking a migration |
| 2 | A task deleted between find and update threw (a 500 in part 4) | Fixed test-first in parts 2 and 3: `UpdateAsync` returns false, the service maps it to not found; integration test with two contexts and a unit test where the fake deletes in between | Agree; to confirm as a 404 over HTTP in part 4 |
| 3 | Unit tests also need Docker | Declined (prompt asks for one container per run, one test project) | Agree |
| 4 | `dotnet ef` uses a placeholder connection string | Deferred to part 4 | Agree |

**Carried to part 4:** the `sub`-without-user case (part 2 I2 — now a 547 from the FK, so a 500 without a fix), the PUT/DELETE race as a 404, and the design-time connection string. **Open for me:** every new migration will have the BOM again; an `.editorconfig` section for `Migrations/*.cs` would settle it.

**Result:** part 3 accepted.

## Part 4 — HTTP and auth (2026-11-11, prompt v5)

**Re-run by me:** `dotnet build` → 0 warnings, 0 errors; `dotnet format --verify-no-changes` → exit 0; `dotnet test` → 162 total, 0 failed (real SQL Server container). Matches the report. No part 5 file exists.

**TDD evidence:** red 1 compile errors (missing types); red 2 62 of 148 failed, almost all "got 404" because nothing was wired. The agent logged two red failures that were its own test mistakes (EF rewrites connection strings, so comparing raw text was wrong) and three stumbles on the way to green (missing JwtBearer reference; `[Produces("application/json")]` turning problem responses into `application/json`; `ValidationProblem()` bypassing its camelCase formatter).

**The three items carried from parts 2 and 3 — all closed, each with a test that failed first:**

| Item | How it was closed | My verdict |
|---|---|---|
| Token whose `sub` is a Guid with no `Users` row (part 2 I2; a 547/500 since part 3) | `SubjectValidatingJwtEvents.TokenValidated` fails the token after the signature, issuer, audience and expiry checks → 401; tests on GET and POST, POST stores nothing | Agree: 401 is right — the token does not identify a known user. The check costs one query per request, uncached so a deleted user loses access at once |
| Task deleted between find and update (part 3) | 404 end to end; a test-only repository deletes the row inside the request | Agree |
| Design-time connection string (part 3 review) | `dotnet ef` reads user secrets or `ConnectionStrings__TaskManagement`; a never-connecting placeholder only when unset or blank; nothing secret committed | Agree; checked with `dotnet ef dbcontext info` by the agent |

**Code read by me:**

| # | Point | Verdict |
|---|---|---|
| 1 | Bearer: HS256 only, signed tokens with an expiry required, issuer/audience/key validated, 30 s skew, `MapInboundClaims = false`, `IncludeErrorDetails = false`, name claim `sub` | OK |
| 2 | Fallback policy requires an authenticated user, plus `[Authorize]` on the controller — a new endpoint cannot be anonymous by accident | OK |
| 3 | `CurrentUser` reads the id only from `sub`; the controller never touches the token or EF | OK |
| 4 | The controller maps `TaskResult` → 200 / 201 + `Location` (`CreatedAtAction`) / 204 / 400 (ValidationProblemDetails, camelCase keys) / 404 (same ProblemDetails for missing and foreign); routes constrained to `{id:guid}` | OK |
| 5 | A new `Http/` folder (strict enum converter and binder) not in the prompt's tree | Accepted — it holds what the prompt asks (reject unknown and numeric enum values); the tree did not foresee it |
| 6 | `context.Fail(...)` messages include the token's `sub` value; not sent to the client (`IncludeErrorDetails = false`) but can reach the authentication log | Minor: a user-supplied value in a log line; acceptable here, noted for the answer |
| 7 | The part 1 smoke test now expects 401 for an unknown path (fallback policy) | OK, logged by the agent |

**The reviewer:** 0 Critical, 0 Important, 6 Minor; confirmed the three items closed. Decisions: enum binder limited to query and route values (fixed test-first); `IncludeErrorDetails` off in every environment (fixed test-first); a user deleted mid-request → clean 500 (declined: the API cannot delete users); blank design-time string falls back (fixed test-first); only an HTTP launch profile (deferred to part 5 as a note); log counts (fixed). I agree with each.

**Real bug found because of the reviewer, by the agent:** the reviewer marked "comma-separated status" as *not verified*; the agent tested it and found that JSON `"status":"Todo, InProgress"` was accepted and stored as `InProgress` — `JsonStringEnumConverter` parses flags syntax even for a non-flags enum. Fixed test-first with a name-only converter shared with the query binder. This is the best example so far for the answer: a plausible line of AI-generated code with a subtle framework behaviour, caught only because "not verified" was treated as a question, not a pass.

**Result:** part 4 accepted. Left for part 5: the HTTPS note and run instructions (user secrets for `Jwt:SigningKey` and the connection string; `dotnet ef database update`).
