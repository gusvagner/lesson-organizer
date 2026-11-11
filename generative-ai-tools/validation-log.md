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

**Result:** part 1 accepted. Before part 2: add `generative-ai-tools/.editorconfig` with `root = true` (environment fix, recorded in `prompt-iterations.md`).
