# 08 -- Testing and Non-Breaking Change Policy

## Purpose

Every new MVP must preserve all previously accepted functionality.

## Test layers

1.  Domain unit tests.
2.  Application service tests.
3.  Repository/database integration tests against PostgreSQL.
4.  API contract tests.
5.  Angular component/service tests.
6.  End-to-end UI tests.
7.  Installer/upgrade tests.
8.  Regression suite across all prior MVPs.
9.  Security and performance tests.

## Required regression suites

-   Authentication and permissions.
-   Company setup.
-   Product/customer/supplier CRUD.
-   Invoice numbering.
-   GST calculations.
-   Document finalization/cancellation.
-   PDF generation.
-   Payment allocations.
-   Customer/supplier outstanding.
-   Purchase posting.
-   Stock movement and valuation.
-   GST report generation.
-   External API state handling.
-   Backup and restore.
-   Upgrade from previous released version.

## Non-breaking development rules

-   Existing accepted API contracts remain compatible.
-   Add fields as optional/defaulted when safe.
-   Do not remove or repurpose existing API fields.
-   Use new API versions for breaking API changes.
-   Database changes must be backward-compatible during rollout.
-   Do not change document numbering behavior for existing series
    without an explicit migration and approval.
-   Do not change historical document snapshots.
-   Do not recalculate historical invoices using current tax rules.
-   Do not alter past stock movements; use compensating transactions.
-   Do not rewrite payment allocation history; use reversal and
    reallocation.
-   Feature-flag new integrations until verified.
-   Preserve settings and user preferences across upgrades.

## Required checks before merge

-   Build succeeds.
-   All unit tests pass.
-   All integration tests pass.
-   Existing regression suite passes.
-   Migration works on empty DB.
-   Migration works on previous supported DB.
-   No new critical/high security issue.
-   UI smoke test passes.
-   Documentation updated.
-   Changelog updated.

## Financial test cases

-   Tax-inclusive and tax-exclusive price behavior if supported.
-   Intra-state and inter-state.
-   Multiple tax rates in one invoice.
-   Discounts at item and invoice level.
-   Rounding and amount-in-words.
-   Credit/debit notes.
-   Partial/advance payments.
-   Multiple allocations.
-   Payment reversal.
-   Cancelled documents.
-   Concurrent invoice finalization.
-   Concurrent stock deduction.
-   Returns and stock reversal.

## Test data

Use deterministic synthetic test data. Never use real taxpayer/customer
secrets in automated tests. Include boundary cases and realistic Indian
GST examples reviewed by domain experts.

## Release evidence

For each MVP, publish: - Build/test summary. - Test case results. -
Migration report. - Regression report. - Screenshots or recording of key
flows. - Known issues. - Rollback/recovery instructions.
