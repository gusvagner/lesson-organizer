# Security Review (stage 13) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Review the finished app against the stage 13 checklist, fix the gaps test-first, and record every item as OK (with proof), Fixed, or Accepted risk in `docs/security-review.md`.

**Architecture:** Three small backend additions close the gaps found while planning: a security headers middleware (with the inline theme script moved to a file so the CSP needs no `unsafe-inline` for scripts), a request body size limit, and quieter EF Core logs with a test that no secret reaches any log. Everything else in the checklist is verified against existing code and tests and cited in the review document.

**Tech Stack:** ASP.NET Core middleware, Kestrel limits, a named capturing `ILoggerProvider` for tests, `dotnet list package --vulnerable`, `npm audit`.

**Spec:** [plan.md §7 "Security review (stage 13)"](../plan.md) (the checklist), [requirements.md §2](../requirements.md#2-authentication-and-data-isolation), [architecture.md](../architecture.md), [frontend-architecture.md](../frontend-architecture.md).

## Global Constraints

- Every fix is test-first: the regression test fails before the change.
- `docs/security-review.md` has one row per checklist item: **OK** (with the test or file that proves it), **Fixed** (test name + commit), or **Accepted risk** (why; also in README "Known limitations").
- The vulnerable-package checks query nuget.org and the npm registry: run only with the human's approval (asked at plan approval).
- No behaviour change visible to a teacher; the browser console stays at 0 errors and 0 warnings (checked again in Edge at the end).
- Everything in English; no explanatory comments in code.

## Review Focus

1. **CSP blocks something the app needs** (Angular component styles, Material inline styles, the theme script, fonts, icons): page breaks or console fills with violations. Checked by the Edge run in Task 5.
2. **Headers missing on some responses** (401 from the fallback policy, static files, `index.html` fallback, ProblemDetails errors). Tests in Task 1 cover each kind.
3. **Body limit too small for valid input** (description 5000 chars + notes 2000 chars as UTF-8): a valid save rejected. Test in Task 2.
4. **A secret in logs at startup** (connection string printed by EF or the host). Test in Task 3 covers startup and a full request flow.
5. **`me` is now anonymous** (changed in plan 5 for a clean console): it must still never return another user's data. Covered by existing tests; cited in Task 5.

---

### Task 1: Security headers

**Files:** Create `src/LessonOrganizer.Api/Http/SecurityHeaders.cs` (`UseSecurityHeaders(this WebApplication app)`), `frontend/public/theme-init.js` (the current inline script, same logic). Modify `Program.cs` (first middleware), `frontend/src/index.html` (`<script src="theme-init.js"></script>` in `<head>`, inline script removed). Test: `tests/LessonOrganizer.Api.IntegrationTests/Security/SecurityHeadersTests.cs`.

**Interfaces:**
- Produces, on every response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'`.

- [ ] **Step 1: failing tests** (theory over responses): `/students/1` (SPA fallback, anonymous), `/favicon.svg` (static), `/api/students` without session (401), `/api/students/{missing}` with session (404 ProblemDetails), `/api/auth/csrf` (204) → each has the four headers with the exact values above.
- [ ] **Step 2–4:** FAIL → implement (headers set in `Response.OnStarting` so error and short-circuit responses get them too) → PASS. `cd frontend && npx ng build` → built `index.html` has no inline `<script>` body.
- [ ] **Step 5: Commit** `feat: security headers and csp; theme init script moved to a file`

### Task 2: Request body size limit

**Files:** Create `src/LessonOrganizer.Api/Http/RequestBodyLimit.cs` (`UseRequestBodyLimit(this WebApplication app, long maxBytes)`: rejects a request whose `Content-Length` exceeds the limit with a 413 ProblemDetails), modify `Program.cs` and `Startup/ApiServiceCollectionExtensions.cs` (`Kestrel:Limits:MaxRequestBodySize` = 1 MiB for chunked bodies), test `tests/LessonOrganizer.Api.IntegrationTests/Security/RequestBodyLimitTests.cs`.

- [ ] **Step 1: failing tests**: `PostStudent_BodyOver1MiB_Returns413WithoutSavingAnything`; `PostLessonPlan_WithLongestValidFields_Returns201` (5000-char description with multi-byte characters, guards Review Focus 3).
- [ ] **Step 2–4:** FAIL → implement → PASS.
- [ ] **Step 5: Commit** `feat: limit request body size to 1 MiB`

### Task 3: Logs never contain secrets

**Files:** Create `tests/LessonOrganizer.Api.IntegrationTests/Security/LogSecretsTests.cs` and `tests/LessonOrganizer.Testing/Logging/CapturingLoggerProvider.cs` (named, collects formatted messages and exception text). Modify `appsettings.json` (`Microsoft.EntityFrameworkCore.Database.Command`: `Warning`), `ApiFactory` (optional logger provider).

- [ ] **Step 1: failing test** `FullFlow_LogsContainNoPasswordHashCookieOrConnectionString`: start the app, register, log in, create a student and a lesson plan, fail one login, log out; assert no captured message contains the password, the stored password hash, the auth cookie value, the XSRF token, or the connection string password. Second assertion `EfCommands_NotLoggedAtInformation` (fails today: EF logs every SQL command at Information).
- [ ] **Step 2–4:** FAIL (second assertion) → change the log level → PASS (if the first assertion fails, fix the leak it finds).
- [ ] **Step 5: Commit** `fix: ef core commands logged only at warning; test that logs hold no secrets`

### Task 4: Dependency audit (needs the human's approval)

- [ ] **Step 1:** `dotnet list package --vulnerable --include-transitive` → record the output.
- [ ] **Step 2:** `cd frontend && npm audit --omit=dev` and `npm audit` → record both outputs.
- [ ] **Step 3:** Any High or Critical in a package the app ships: upgrade test-first where possible; otherwise record as accepted risk with the reason. Dev-only findings (build and test tools) are recorded, not shipped.
- [ ] **Step 4: Commit** (only if something changed) `fix: upgrade vulnerable packages`

### Task 5: Review document, docs and final check

**Files:** Create `docs/security-review.md`. Modify `docs/plan.md` (stage 13 checklist: `me` is anonymous and returns 204 without a session; headers row), `README.md` ("Known limitations" final), `docs/architecture.md` (middleware order), `.gitignore` (`.superpowers/`), `docs/ai/AI_LOG.md`.

- [ ] **Step 1:** One row per checklist area (Authorization, Data isolation, Session cookie, CSRF, Passwords, Brute force, Input, Errors and logs, Database, Transport and headers, Frontend, Secrets and demo data, Dependencies), each OK / Fixed / Accepted risk with the test name or file as evidence. Accepted risks: demo over HTTP on localhost (Safari will not keep the `Secure` cookie), `sa` login, data protection keys unencrypted in the volume, lockout by a third party, last write wins, rate limit per IP and instance.
- [ ] **Step 2:** `dotnet test` and `cd frontend && npx ng test --watch=false` → all pass; rebuild the demo (`docker compose up -d --build app`) and run the three workflows in Edge again → 0 console errors or warnings (CSP included).
- [ ] **Step 3: Commit** `docs: security review`
