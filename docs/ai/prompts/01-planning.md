# Prompt 01 — Planning

- **Tool:** Claude Code (Claude Opus 5.5)
- **Date:** 2026-11-09
- **Goal:** Get an implementation plan before any code is written.

## Prompt

The prompt was the full text of [`docs/requirements.md`](../../requirements.md) (sections 1–10), followed by section 11 below. The requirements file was edited after the review (see "Human review"); the differences from the text actually sent are:

- Intro was written in first person ("I want to build a web application for an independent English teacher…"); rewritten as a product description plus a persona.
- Section 5 asked for a UI "in Portuguese"; changed to English.

Section 11, as sent:

```markdown
## 11. Expected output at this stage

Before writing any code, provide:
1. A concise understanding of the application.
2. Any ambiguities or inconsistencies identified.
3. The proposed data model and relationships.
4. The proposed architecture.
5. API contracts and endpoints.
6. Acceptance criteria.
7. An implementation plan broken into small, verifiable stages.

Do not expand the scope.
Clearly label any optional suggestions as optional.
Wait for my approval before implementing.
```

## Prompt design notes

- Requirements are explicit about edge cases (404 vs 409, ownership, whitespace), so the model has less to guess.
- Explicitly says "plan only, wait for approval", so no code is generated before review.
- Out-of-scope list and "keep abstractions proportional" stop the model from over-engineering.
- "Never claim that a test or command was executed if it was not actually run" guards against fabricated results.

## Output

[`docs/plan.md`](../../plan.md)

## Human review

| Plan item | Decision |
|---|---|
| A1 — UI language | **Changed.** The model kept the Portuguese UI from the requirements and flagged the conflict. Decided: the whole project, UI included, is in English. |
| A3 — foreign or missing student on create/update returns 400 with the same message | Accepted. |
| A4 — filtering by another teacher's student returns an empty list | Accepted. |
| A9 — registration signs the teacher in | **Rejected.** Registration only creates the account; the teacher logs in afterwards. |
| Requirements wording | **Changed.** First-person intro rewritten as a product description with an explicit persona. |
