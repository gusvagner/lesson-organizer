# Lesson Organizer

Web app for independent English teachers to manage students and lesson plans.

## What it is

To be written in stage 12.

## Screenshots

To be written in stage 12.

## Prerequisites

To be written in stage 12.

## Run it

To be written in stage 12.

## Demo credentials

Seeded at startup when `Seed:DemoData` is `true` (on in Development and in the Docker demo) and the database has no users. Dates are relative to the day the seed runs.

| Teacher | Email | Password | Students | Lesson plans |
|---|---|---|---|---|
| Maria Silva | `maria@example.com` | `Demo1234!` | Ana (Intermediate), Bruno (Beginner), Carla (Advanced) | Ana: "Present perfect — conversation" (Delivered, 7 days ago), "Phrasal verbs at work" (Ready, in 2 days). Bruno: "Greetings and introductions" (To prepare, in 5 days). Carla: none. |
| John Smith | `john@example.com` | `Demo1234!` | David (Elementary), Emma (Upper intermediate) | David: "Past simple stories" (Delivered, 3 days ago), "Ordering at a restaurant" (To prepare, in 4 days). Emma: "Job interview practice" (Ready, tomorrow). |

Try: deleting Ana is refused because she has lesson plans; Carla can be deleted; John sees none of Maria's data.

## Tests

To be written in stage 12.

## Architecture

To be written in stage 12. See [docs/architecture.md](docs/architecture.md).

## API

JSON over HTTPS, cookie session, errors as RFC 9457 ProblemDetails. Full contracts in [docs/plan.md §5](docs/plan.md#5-api-contracts). In Development, the OpenAPI document is at `/openapi/v1.json` and the Scalar UI at `/scalar`.

| Area | Endpoints | Auth |
|---|---|---|
| Auth | `GET /api/auth/csrf`, `POST /api/auth/register`, `POST /api/auth/login` | public |
| Auth | `POST /api/auth/logout`, `GET /api/auth/me` | session |
| Students | `GET /api/students?search=`, `GET/PUT/DELETE /api/students/{id}`, `POST /api/students` | session |
| Lesson plans | `GET /api/lesson-plans?studentId=&status=`, `GET/PUT/DELETE /api/lesson-plans/{id}`, `POST /api/lesson-plans`, `PATCH /api/lesson-plans/{id}/status` | session |

Unsafe requests (POST, PUT, PATCH, DELETE) need the `X-XSRF-TOKEN` header with the value of the `XSRF-TOKEN` cookie that `GET /api/auth/csrf` sets; call it again after login and logout. Another teacher's data answers 404, like missing data.

## Decisions

To be written in stage 12. See [docs/decisions/](docs/decisions/).

## AI usage

To be written in stage 12. See [docs/ai/AI_LOG.md](docs/ai/AI_LOG.md).

## Known limitations

To be written in stage 13.
