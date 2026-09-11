# AGENTS.md

## Purpose

This file defines how a coding agent should work in this repository.

This repo is a server-rendered ASP.NET Core MVC application for document management. It uses Razor Views, controllers, EF Core with PostgreSQL, session state, Serilog, and some Google Cloud Storage integration.

The agent should make the smallest clean change that matches the existing codebase and keeps the app easy to maintain.

## Working Defaults

* Prefer simple, explicit code over flexible abstractions.
* Prefer small focused edits over broad rewrites.
* Follow existing repository patterns unless they are clearly harmful.
* Keep controllers thin and put business logic in services.
* Keep data access in EF Core and repository classes already used by the project.
* Avoid introducing new packages unless the platform or current dependencies cannot solve the problem well.
* Do not add comments that restate obvious code.

## Repository Shape

Use the current structure as the default:

* `Controllers/` for MVC controllers and HTTP entry points
* `Service/` for business logic, orchestration, middleware, and external integrations
* `Repository/` for data access helpers already used by the app
* `Data/` for `ApplicationDbContext`, seeding, and EF-related setup
* `Models/` for entities and view models
* `Views/` for Razor views
* `Migrations/` for EF Core migrations

## Architectural Rules

* Keep business rules out of Razor views and controllers.
* Put validation close to the request boundary or service boundary where it is easiest to find.
* Use interfaces where the repo already benefits from them, especially for service seams and external integrations.
* Do not create interfaces for every class by default.
* Do not add pass-through service layers with no behavior.
* Do not add generic repository wrappers over EF Core.
* Prefer concrete, readable flows over clever generic helpers.

## Coding Style

* Read the applicable `.editorconfig` rules before editing a file. `.editorconfig` is the source of truth for formatting, naming, and C# style; it takes precedence over style guidance in this file or surrounding code.
* Follow the effective rules for the file, including file-pattern sections and any nested `.editorconfig` overrides. Do not duplicate their settings here, so future configuration changes remain authoritative.
* Apply configured style preferences to new and changed code, including preferences with suggestion or silent severity. Severity controls diagnostics, not whether the preference applies.
* Use existing conventions and the simplicity guidelines below where `.editorconfig` does not specify a preference.
* Use clear names and shallow control flow.
* Prefer guard clauses and early returns.
* Keep methods focused.
* Preserve nullable correctness.
* Use async end-to-end for I/O-bound work.
* Do not block async code with `.Result`, `.Wait()`, or similar patterns.
* Follow `.editorconfig` for namespace declarations, braces, constructors, expression bodies, imports, and other syntax choices.
* Keep one public type per file unless a small local grouping is clearly simpler.

## MVC and HTTP Guidance

* This repo is controller-and-view based. Default to extending existing controllers and Razor views.
* Keep controller actions focused on request handling, authorization decisions, model binding, and choosing the response.
* Put non-trivial query logic, storage workflows, and access rules into services or existing repository classes.
* Preserve existing route patterns unless the task requires a route change.
* Return consistent HTTP results and avoid inventing custom response wrappers unless the repo already expects them.

## Data and Persistence

* Prefer EF Core patterns already present in the repo.
* Keep queries explicit and easy to reason about.
* Add migrations only when schema changes are required for the task.
* Keep each migration focused on one logical schema change.
* Make destructive schema changes explicit.
* Respect PostgreSQL behavior already configured by the app.

## Security and Operations

* Validate and sanitize external input.
* Do not log secrets, tokens, credentials, or connection strings.
* Preserve authentication, authorization, session, and maintenance-mode behavior unless the task requires changing them.
* Be careful with file handling and cloud storage paths. Prefer existing workflow services over duplicating storage logic.

## Dependencies

* Prefer the .NET base class library and existing Microsoft packages first.
* Reuse existing packages already in the repo when reasonable.
* Add a third-party package only with clear justification.
* Remove unused code or dependencies only when directly relevant to the task.

## Tests and Verification

There is currently no separate test project in this repository. When changing behavior:

* Add automated tests if a test project exists or if the task includes creating one.
* If no test project exists, do not fabricate a large test harness just to satisfy process.
* At minimum, run the relevant build or targeted verification commands when possible.

Default validation commands:

* `dotnet build`
* `dotnet test` only if a test project exists

If a schema change is made, also verify the relevant EF Core migration artifacts.

## Change Discipline

When making changes, follow this order:

1. Understand the requirement.
2. Inspect the existing code path and conventions.
3. Make the smallest clean change that solves the problem.
4. Update tests when practical and appropriate.
5. Run relevant validation.
6. Avoid unrelated refactors.

## Avoid By Default

Do not introduce these unless clearly justified by the task:

* Generic repository abstractions over EF Core
* Marker interfaces
* Deep inheritance hierarchies
* Static global mutable state
* Reflection-heavy or dynamic designs
* Placeholder abstractions for future requirements
* Broad folder reorganizations
* Cosmetic-only churn

## Final Rule

Prefer the solution that is easiest for the next maintainer to read, test, and change while staying aligned with this repository's current MVC and EF Core structure.
