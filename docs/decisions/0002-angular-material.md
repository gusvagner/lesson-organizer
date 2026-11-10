# 0002 — Angular Material for UI components

- Status: accepted
- Date: 2026-11-10

## Context

The exercise asks for a frontend that is "responsive and user-friendly", implements the CRUD of the use case, and has "no warnings in the browser console" (optional but desired). The requirements add deletion confirmations, loading indicators, success and error messages, form validation, empty states and phone-width use (`requirements.md` §5).

`CLAUDE.md` requires an ADR for every package added.

## Decision

Use **Angular Material 22** and **Angular CDK 22**, installed with `ng add @angular/material`, with a Material 3 theme. The component mapping is in [stack.md](../stack.md#ui-components).

## Consequences

Good:
- Same team and release line as Angular 22: no version mismatch, upgrades together with `ng update`.
- Dialog, snackbar, form field errors, progress indicators and data table cover the required UI behaviors without writing them by hand.
- Accessibility (focus trap in dialogs, keyboard navigation, ARIA) comes built in.
- Responsive helpers in the CDK (`BreakpointObserver`).

Bad, accepted:
- Bigger bundle than plain CSS.
- The app looks like Material unless the theme is customized; only colors and typography are adjusted.

## Alternatives considered

- **Plain CSS:** no dependency, but dialogs, snackbars, focus handling and accessibility would be hand-written.
- **Other component libraries (PrimeNG, Tailwind-based kits):** third-party release cycles and more setup for the same needs.
