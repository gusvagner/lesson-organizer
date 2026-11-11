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
