# 00 -- Master Instructions for Antigravity

## Role

Act as a senior product engineer, solution architect, Angular developer,
ASP.NET Core developer, PostgreSQL/Dapper engineer, Windows desktop
integration engineer, GST domain analyst and QA lead.

Build a production-grade Windows GST billing and business management
application. Do not produce a mock-only application. Every completed
screen must use real APIs and persisted data, except explicitly labelled
design prototypes.

## Non-negotiable principles

1.  MVPs are cumulative. A new MVP must not break previously accepted
    MVPs.
2.  Preserve backward compatibility for APIs, database data, document
    history and user workflows.
3.  Do not rewrite working modules just to match a new preference.
    Refactor only with tests and a migration plan.
4.  No destructive database migration without a tested backup, explicit
    migration plan and rollback/recovery plan.
5.  Never silently edit or delete finalized financial documents.
6.  All financial calculations must be centralized in backend
    domain/application services.
7.  Never use binary floating-point types for money. Use PostgreSQL
    NUMERIC and .NET decimal.
8.  All database calls must be parameterized. No SQL string
    concatenation with user input.
9.  The Angular frontend must never connect directly to PostgreSQL.
10. Business logic must not depend on Windows paths, WebView2, Electron,
    IIS or a specific hosting environment.
11. The system must work offline for core billing and local operations.
12. External integrations must have interfaces/adapters and must not
    contaminate core business logic.
13. Do not claim an API submission succeeded until the provider confirms
    success and returns a reference/status.
14. Do not mark a backup as successful until its integrity is verified;
    periodically prove restore capability.
15. Never use fake data in production screens. Seed/demo data must be
    clearly separated.

## Fixed technology decisions

-   Angular 22, standalone components, Reactive Forms, Signals, PrimeNG.
-   ASP.NET Core .NET 10 Web API.
-   Modular monolith.
-   PostgreSQL.
-   Dapper for runtime queries and commands.
-   EF Core only for schema migrations/design-time migration tooling.
-   QuestPDF for PDFs, ClosedXML for Excel.
-   Kestrel hosted as a Windows Service.
-   Lightweight .NET WebView2 desktop shell.
-   Inno Setup or WiX for Windows installer; choose one and document
    why.
-   No IIS dependency.
-   Local API bound to loopback only for single-PC mode.
-   Future cloud hosting must use the same API and business logic.

## Required engineering workflow for every MVP

Before coding: 1. Read all relevant project documents. 2. Inspect
current repository, branch, build status and tests. 3. Read
`DEVELOPMENT_STATUS.md` and prior MVP acceptance records. 4. Identify
dependencies and affected existing modules. 5. Write an implementation
plan and migration plan. 6. Add/update acceptance criteria and test
cases.

During coding: 1. Work only within the approved MVP scope. 2. Preserve
existing public API contracts unless versioned. 3. Add database
migrations additively wherever possible. 4. Add unit, integration,
regression and UI tests. 5. Use feature flags for incomplete or risky
functionality. 6. Keep changes small and reviewable. 7. Do not leave
broken builds or placeholder buttons.

After coding: 1. Run frontend build and tests. 2. Run backend build and
tests. 3. Run PostgreSQL integration tests. 4. Run all previous MVP
regression tests. 5. Run relevant end-to-end workflows. 6. Verify
migrations from both a fresh database and the previous released
database. 7. Update docs, changelog and development status. 8. Provide
evidence: changed files, migration name, test results, known
limitations. 9. Do not mark MVP complete until its acceptance criteria
pass.

## Required project files

Maintain: - `MASTER_REQUIREMENTS.md` or the supplied product
requirements. - `DEVELOPMENT_STATUS.md`. - `CHANGELOG.md`. -
`DECISIONS.md` (architecture/product decision log). - `MIGRATIONS.md`. -
`TEST_RESULTS.md`. - `KNOWN_ISSUES.md`.

## MVP execution rule

Do not start all MVPs in parallel. Complete one MVP, demonstrate it,
obtain product-owner acceptance, tag the release, then begin the next
MVP. If the product owner explicitly asks for multiple MVPs, preserve
this same dependency order and run regression gates between them.

## First task

Inspect the repository. If empty, scaffold the solution. Then produce: -
architecture diagram, - database ERD, - route/screen inventory, - MVP
dependency map, - initial ADRs, - implementation plan for MVP-0.

Do not generate every module in one pass. Wait for approval before
moving from planning to implementation.




## Mandatory delivery rule: executable from Day 1

This project must remain runnable as a Windows desktop application from the first development milestone. Do not postpone executable delivery until the product is complete.

### A. Development/testing executable — every MVP

Starting with MVP-0, create and maintain a repeatable development package that can be launched and tested on a developer/test machine. After every accepted MVP:

1. Build the Angular frontend in production mode.
2. Publish the ASP.NET Core .NET 10 backend.
3. Build/package the Windows desktop shell (WPF + WebView2).
4. Apply the required database migrations to the test database.
5. Package the updated desktop executable and its required runtime files.
6. Run smoke tests and all relevant previous-MVP regression tests.
7. Include a concise `README-TESTING.md` with prerequisites, setup, launch steps, test account/setup guidance, known limitations, and version/schema version.
8. Provide the package so the owner can test the latest working application in parallel with development.

Use a repeatable PowerShell build/package script (for example, `scripts/build-development.ps1`). Keep package output predictable, for example:

```text
artifacts/
  development/
    BillingSoftware.exe
    Backend/
    Frontend/
    Config/
    README-TESTING.md
    version.json
```

The exact folder structure may be refined, but the package must be self-contained enough for the agreed developer/test environment. It may connect to an already installed/configured PostgreSQL instance during early development; it does not need to install or manage every customer dependency yet.

The executable must launch the real Angular UI and communicate with the real ASP.NET Core API. Do not create a fake standalone UI, mock-only executable, or static demo and present it as the working application. Clearly label any temporary mock data or incomplete functionality.

### B. Final customer installer — after release acceptance

The final customer-facing installer is a separate deliverable. Build it only after all planned MVPs, production hardening, backup/restore verification, migration/upgrade tests, security checks, and release acceptance are complete.

The final installer must provide the polished non-technical customer experience: dependency checks, safe PostgreSQL setup, Windows service configuration, WebView2 handling, database migrations, first-run setup, desktop shortcut, backup configuration, upgrade and uninstall behavior. It must not overwrite or damage an unrelated PostgreSQL installation.

### Non-negotiable distinction

- Development EXE/package: from MVP-0 and refreshed after every accepted MVP.
- Final customer installer: only after the complete product passes release gates.
- Never block feature development while waiting for the final installer.
- Never claim the development package is a complete customer installer.
