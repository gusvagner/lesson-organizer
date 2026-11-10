# Lesson Organizer — Requirements

Lesson Organizer is a web application for independent English teachers to manage their students and organize lesson plans.

It is a simple planner, similar to a task manager, adapted to preparing private English lessons. It is not a full school management system.

## 1. Goal and user story

Primary persona: an independent English teacher who gives private lessons.

As an independent English teacher, I want to register my students and organize their lesson plans so that I can track what needs preparation, what is ready, and which lessons have been delivered.

The application will have three main entities:
- Teacher/authenticated user.
- Student.
- Lesson plan.

Only teachers will have accounts. Students will be records managed by their teacher and will not have access to the application.

## 2. Authentication and data isolation

The application must support:
- Teacher registration.
- Login.
- Logout.
- Persistent user storage.

Each teacher must only access their own students and lesson plans.

The teacher identifier must come from the authenticated user on the backend. Never accept an arbitrary teacher identifier from the request body.

When a requested resource does not exist or belongs to another teacher, return 404 for read, update, and delete operations.

Registration and login endpoints are public. Student and lesson plan endpoints require authentication.

## 3. Student entity

Fields:
- Id: unique identifier.
- TeacherId: identifier of the teacher who owns the student record.
- Name: required, up to 150 characters.
- Email: optional, up to 254 characters.
- EnglishLevel: the student's English proficiency level.
- Notes: optional, up to 2,000 characters.

Allowed proficiency levels:
- Beginner
- Elementary
- Intermediate
- UpperIntermediate
- Advanced

Rules:
- Name must not be empty or contain only whitespace.
- Email must have a valid format when provided.
- EnglishLevel must be one of the allowed values.
- Do not require unique names or email addresses, as they are not used for identity or login.
- Each student belongs to exactly one teacher.
- A student can have multiple lesson plans.

Operations:
- Create a student.
- List my students.
- View student details.
- Update a student.
- Delete a student.

Deletion:
- Allow permanent deletion if the student has no lesson plans.
- If associated lesson plans exist, prevent deletion and return 409 with a clear message.
- Do not cascade-delete lesson plans.
- Do not implement soft deletion in this version.

## 4. Lesson plan entity

Suggested entity name: LessonPlan.

Fields:
- Id: unique identifier.
- StudentId: identifier of the associated student.
- Title: required, up to 150 characters.
- Description: optional lesson content or outline, up to 5,000 characters.
- LessonDate: required planned lesson date.
- Status: the lesson plan's current state.

Allowed statuses:
- ToPrepare
- Ready
- Delivered

Rules:
- Title must not be empty or contain only whitespace.
- The associated student must exist and belong to the authenticated teacher.
- Lesson plan ownership is determined by the teacher who owns the associated student.
- New lesson plans start with the ToPrepare status.
- Allow changes between any valid statuses, including reverting a status to correct a mistake.
- LessonDate represents a calendar date without a time component.
- Allow past dates so teachers can record or correct previous lesson plans.
- Do not implement scheduling conflicts, as this is not a booking system.
- Allow changing the associated student, provided the new student also belongs to the authenticated teacher.

Operations:
- Create a lesson plan for a student.
- List my lesson plans.
- View lesson plan details.
- Update the title, description, student, and date.
- Change the status.
- Permanently delete a lesson plan.

## 5. User interface behavior

The interface must be simple, responsive, and in English.

### Authentication
- Login page.
- Registration page.
- Logout option.

### Students
- List showing name, email, and proficiency level.
- Search by name.
- Create and edit form.
- Deletion confirmation.
- Student details with associated lesson plans.
- An action to create a lesson plan with the student already selected.

### Lesson plans
- Main page showing all lesson plans belonging to the authenticated teacher.
- Each item displays its title, student, date, and status.
- Filters by student and status.
- Default sorting by lesson date in ascending order.
- Create and edit form.
- Student selection restricted to the teacher's own students.
- Status updates.
- Deletion confirmation.

If no students have been registered, guide the teacher to create a student before creating a lesson plan.

If a search or filter returns no results, display an appropriate empty state.

Include:
- Loading indicators.
- Success and error messages.
- Form validation.
- Prevention of duplicate submissions while an operation is in progress.

## 6. Example workflows

Example 1:
- Register a student named Ana with the Intermediate level.
- Create a lesson plan titled "Present perfect — conversation."
- Associate the lesson plan with Ana.
- Enter the planned date and lesson outline.
- Save it with the initial ToPrepare status.
- Mark it as Ready after preparing the materials.
- Mark it as Delivered after teaching the lesson.

Example 2:
- Open Ana's details.
- View her associated lesson plans.
- Create a new lesson plan directly from that page.

Example 3:
- Attempt to delete Ana while she has associated lesson plans.
- The application must prevent deletion and explain why.

## 7. Backend and quality requirements

Use:
- C# and ASP.NET Core Web API with controllers.
- Clean Architecture, separating domain, application, infrastructure, and API concerns.
- SQL Server through EF Core, run with Docker Compose, kept behind repository interfaces so the business logic does not depend on it.
- Angular for the frontend.
- Authentication using a standard ASP.NET Core mechanism, without implementing custom password hashing or cryptographic algorithms.
- Unit and integration tests.

Frontend validation should improve the user experience, but the backend must enforce all rules.

Use dedicated request and response contracts rather than exposing persistence entities directly.

Propose a complete authentication approach and justify it in the plan, including session credential storage, logout behavior, and necessary security protections.

Keep abstractions proportional to the project's size. Do not introduce libraries, patterns, or layers without a concrete need.

## 8. Tests and acceptance criteria

Cover at least:
- Registration and authentication.
- Unauthenticated access to protected endpoints.
- Student CRUD.
- Lesson plan CRUD.
- Required fields and length limits.
- Invalid email addresses.
- Invalid proficiency levels and statuses.
- Creating a lesson plan with a nonexistent student.
- Attempting to associate a lesson plan with another teacher's student.
- Data isolation between two teachers across all operations.
- Preventing deletion of a student with associated lesson plans.
- Status changes.
- Data persistence.

Develop incrementally using TDD:
1. Write tests for the expected behavior.
2. Run them and confirm the expected failure.
3. Implement the behavior.
4. Run the tests again.
5. Refactor while keeping the tests passing.

## 9. Demo and documentation

Prepare:
- Two demo teacher accounts with separate data.
- Students and lesson plans in different statuses.
- Documented demo credentials.
- Idempotent seed data restricted to development/demo environments.
- A README covering setup, execution, and tests.
- A record of relevant prompts, AI-generated output, human review, and validation performed.

Never claim that a test or command was executed if it was not actually run.

## 10. Out of scope

Do not implement:
- Student login.
- Payments or monthly fees.
- Lesson packages.
- Attendance tracking.
- Recurring lessons.
- A drag-and-drop calendar.
- Notifications.
- External integrations.
- Material uploads.
- Video conferencing.
- A chatbot or AI features inside the product.
- Schools, teams, or multiple administrative roles.
