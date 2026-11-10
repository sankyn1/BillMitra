# GEMINI.md -- Permanent Project Instructions

You are the implementation agent for this repository.

Before making any code change: 1. Read `README.md`. 2. Read
`00_MASTER_INSTRUCTIONS.md`. 3. Read the relevant product, architecture,
MVP, database, UI, testing and deployment documents. 4. Read
`DEVELOPMENT_STATUS.md`, `CHANGELOG.md` and `DECISIONS.md` if they
exist. 5. Inspect the current code and tests. Never assume the
repository is empty or that a prior implementation is disposable.

## Product invariants

-   Windows desktop-first, local-first, cloud-ready.
-   Angular 22 + ASP.NET Core .NET 10 + PostgreSQL.
-   Dapper for runtime data access; EF Core migrations only.
-   Modular monolith; no microservices unless explicitly approved.
-   Kestrel Windows Service; no IIS dependency.
-   WebView2 desktop shell.
-   Core billing works offline.
-   One business per initial installation.
-   Full inventory scope.
-   GST reports/export plus authorized submission integration.
-   Manual and API E-Way Bill; eligible E-Invoice/IRP integration.
-   Local and cloud backup.

## Never break previous MVPs

Every MVP is cumulative. Before implementation, list affected earlier
MVPs and tests. After implementation, run the full regression suite for
all completed MVPs. A new MVP is not complete if an earlier accepted
workflow breaks.

Use additive database migrations and backward-compatible API changes.
For breaking changes, propose a versioned migration plan and wait for
approval. Do not delete or rewrite historical financial, payment, stock
or audit records.

## Financial and statutory integrity

-   Use decimal/Numeric for money.
-   Allocate document numbers server-side and atomically.
-   Validate GST invoice numbering rules and current applicable GST
    requirements.
-   Finalized documents are immutable.
-   Use cancellation, credit/debit adjustment or reversal workflows.
-   Maintain document snapshots.
-   Make payment allocation and stock posting transactional.
-   Never fabricate GST submission, IRN or E-Way Bill success.
-   Keep statutory rules effective-dated and reviewable.

## UX

Design for non-technical business owners. Use clear language, consistent
navigation, fast billing workflows, keyboard support, accessible
contrast, inline validation and helpful empty/error states. No inert
buttons or fake production data.

## Cloud portability

Keep business logic independent of Windows, local paths and desktop
shell. Use storage, backup, notification and government-integration
provider interfaces. Frontend communicates only with API. Use
configuration and secret providers; do not hard-code credentials.

## Required response after each task

Report: - Scope completed. - Files changed. - Database migrations. -
API/UI changes. - Tests run and results. - Regression results for
earlier MVPs. - Known limitations. - Documentation updated. -
Recommended next task.

Do not claim completion without test evidence. Do not begin the next MVP
until the current MVP acceptance criteria are met and product-owner
approval is recorded.




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
