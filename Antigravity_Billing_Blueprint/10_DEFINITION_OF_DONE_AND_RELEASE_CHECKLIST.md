# 10 -- Definition of Done and Release Checklist

## Feature definition of done

A feature is complete only when: - Acceptance criteria are met. - UI is
implemented and usable. - API and backend logic are implemented. -
Database migration is included where needed. - Validation and
authorization are implemented. - Audit requirements are implemented. -
Unit/integration/UI tests pass. - Regression suite passes. - Loading,
empty, success and error states exist. - Documentation and changelog are
updated. - No placeholder action remains.

## MVP definition of done

-   All MVP stories accepted.
-   All P0 tests pass.
-   Previous MVP regression suite passes.
-   Fresh database setup passes.
-   Upgrade from previous MVP/release passes.
-   Backup/restore tested where data schema changes.
-   Security review completed.
-   Product owner demonstration completed.
-   Known issues documented.
-   Release tag created.

## Production release gates

-   Clean Windows 10/11 installation.
-   No manual IIS configuration.
-   No manual Node.js/SDK requirement.
-   Application starts after reboot.
-   Database and API health checks work.
-   Core billing works offline.
-   Financial calculations validated.
-   Invoice number rules validated.
-   GST reports reconciled.
-   External integration status is authoritative.
-   Inventory ledger reconciles.
-   Backup restore proven.
-   Upgrade preserves data.
-   Uninstall preserves data by default.
-   No critical/high security defects.
-   Support diagnostics can be exported safely.

## Release notes template

-   Version:
-   Release date:
-   MVPs included:
-   New features:
-   Improvements:
-   Database migrations:
-   API changes:
-   Installer changes:
-   Backup/restore changes:
-   Tests executed:
-   Known issues:
-   Upgrade instructions:
-   Rollback/recovery instructions:


## Additional Definition of Done: executable delivery

For MVP-0 and every subsequent MVP, the following are mandatory acceptance items:

- [ ] Updated Windows development/testing executable package is produced.
- [ ] Package launches the actual desktop shell and real Angular frontend.
- [ ] Frontend communicates with the actual ASP.NET Core API.
- [ ] Database migrations are applied and versioned.
- [ ] Repeatable PowerShell build/package script is updated.
- [ ] Smoke tests and all relevant regression tests pass.
- [ ] `README-TESTING.md` documents setup, launch, limitations, and version.
- [ ] Package is clearly labelled as a development/testing build, not the final customer installer.

The final customer installer is not required for each MVP. It is produced only after all MVPs and production release gates are accepted.
