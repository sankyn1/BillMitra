# 09 -- Windows Installer, Backup and Updates

## Customer experience

The customer downloads one installer, runs it, follows a simple wizard
and opens the desktop shortcut. No manual IIS, Node.js, .NET SDK or
database configuration.

## Installer responsibilities

-   Check Windows version/architecture.
-   Check privileges and disk space.
-   Install WebView2 runtime if required.
-   Install/configure PostgreSQL dedicated instance if required.
-   Deploy self-contained .NET backend.
-   Deploy Angular production assets.
-   Install desktop shell.
-   Create protected data directories.
-   Create restricted database role/database.
-   Apply migrations.
-   Register Windows Service.
-   Configure startup/recovery.
-   Configure local API port.
-   Create desktop shortcut.
-   Run health check.
-   Launch application.

## Data locations

-   Program files: `C:\\Program Files\\BillingSoftware\\`
-   Shared app data: `C:\\ProgramData\\BillingSoftware\\`
-   Logs: `C:\\ProgramData\\BillingSoftware\\Logs\\`
-   Documents: configurable under ProgramData or selected data drive.
-   Default backups: `D:\\BillingSoftware\\Backups\\`

Never store business data in Program Files. Support choosing another
backup path if D: is absent.

## PostgreSQL

-   Use a dedicated database instance/service or a carefully isolated
    managed installation strategy.
-   Do not overwrite an unrelated PostgreSQL installation.
-   Do not expose database port publicly.
-   Use a restricted runtime DB role.
-   Keep superuser credentials out of application settings.
-   Protect service data directory permissions.

## Backup strategy

-   PostgreSQL base backup plus WAL archiving for point-in-time
    recovery.
-   Keep base backup and corresponding WAL chain together.
-   Use checksums/manifests.
-   Encrypt off-device backup before upload.
-   Retention policies configurable.
-   Warn when backup target is unavailable or low on space.
-   Never copy the live PostgreSQL data directory as a backup.
-   Verify backups and perform scheduled restore drills.

## Restore

-   Show backup date, size, source and verification state.
-   Validate manifest and WAL chain.
-   Require explicit confirmation.
-   Create safety backup before replacing active data.
-   Restore to an isolated target first where practical.
-   Verify database integrity and application health.
-   Record operator, source, target and result.
-   Never silently overwrite current business data.

## Cloud storage

Implement provider abstraction. Start with one selected S3-compatible
provider only after provider choice and credentials are available. Do
not hard-code provider into backup orchestration.

## Upgrade

1.  Detect current version.
2.  Check compatibility.
3.  Verify disk space.
4.  Create safety backup.
5.  Stop application services.
6.  Apply compatible migrations.
7.  Replace application files.
8.  Start services.
9.  Run health checks.
10. Report success/failure clearly.

Do not automatically perform destructive downgrade. Recovery should use
a verified backup and documented restore procedure.

## Uninstall

Default uninstall removes application binaries/services but preserves
database, documents, settings and backups. Offer separate explicit data
removal with strong confirmation and a final export/backup reminder.


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
