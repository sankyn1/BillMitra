# 03 -- MVP Roadmap and Prioritized Backlog

## Delivery rules

MVPs are cumulative and dependency ordered. Each MVP must leave the
product buildable, testable and usable. Do not start the next MVP until
the current MVP passes acceptance and regression gates.

Priority: - P0: Required for initial production release. - P1: Important
follow-up. - P2: Advanced. - P3: Future.

## MVP-0 -- Engineering foundation

Deliver: - Repository and solution structure. - Angular, .NET and
PostgreSQL baseline. - CI/local build scripts. - Configuration and
logging. - Health checks. - Dapper connection factory. - EF Core
migration project/tooling. - API versioning and OpenAPI. - Automated
test projects. - Architecture tests. Acceptance: - Clean checkout
builds. - Database can be created from migrations. - API health endpoint
works. - No secrets committed. - Frontend can call local API. - Initial
architecture and decisions documented.

## MVP-1 -- Windows shell, installer and first-run setup

Deliver: - WebView2 desktop shell. - ASP.NET Core Windows Service. -
PostgreSQL automated installation/configuration. - One Windows
installer. - Service health checks and startup recovery. - First-run
company setup wizard. - Initial owner account. - Protected data
directories. Acceptance: - Clean Windows 10/11 x64 machine installs
without manual IIS, Node or database setup. - Application opens from
desktop shortcut. - Business data survives restart and reinstall. - Core
UI opens while offline. - Installer does not overwrite an existing
customer database.

## MVP-2 -- Company, users, products and customers

Deliver: - Company profile and GST registration type. - Financial year
configuration. - User/role/permission baseline. - Product and service
catalogue. - Units, categories, HSN/SAC and tax-rate master. - Customer
and supplier masters. - Excel import/export. Acceptance: - CRUD and
validation work. - Archived records remain available to historical
documents. - Search/filter/pagination work. - Imported data validation
report is available. - Existing MVP-1 setup and authentication
regression tests pass.

## MVP-3 -- GST calculation and sales documents

Deliver: - Central GST calculation service. - Draft/finalized/cancelled
document lifecycle. - Tax invoice, bill of supply, quotation, proforma,
credit note, debit note, delivery challan. - Document numbering
engine. - PDF/print templates. - Document conversion and references.
Acceptance: - Tax calculations pass approved test matrix. - Finalized
document snapshot is immutable. - Number allocation is atomic and
concurrency-safe. - Quotation/proforma conversion does not duplicate
documents. - Credit/debit notes retain original reference. - Previous
MVP regression suite passes.

## MVP-4 -- Payments, ledger and outstanding

Deliver: - Receipts and payments. - Full/partial/advance payments. -
Payment allocation to multiple documents. - Unallocated balances. -
Customer/supplier ledger. - Outstanding and aging. - Payment terms and
due-date reminders. Acceptance: - Payment allocation cannot exceed
available payment or document balance. - Outstanding reconciles to
invoices, adjustments and allocations. - Reversals are auditable and
transactional. - Existing invoice totals and documents remain unchanged.

## MVP-5 -- GST reporting and submission foundation

Deliver: - Sales/purchase GST registers. - GSTR-1 preparation/export. -
GSTR-3B summary. - HSN summary. - GSTR-2B import/reconciliation
foundation. - GST liability and filing tracker. - Provider adapter
interface and sandbox workflow. Acceptance: - Reports reconcile to
source documents. - Export validation errors are explained. - Direct
submission status is only marked successful after provider
acknowledgement. - Submission attempts and acknowledgements are
auditable. - Rules are configurable/versioned and reviewed against
current GST requirements.

## MVP-6 -- E-Way Bill and E-Invoice integrations

Deliver: - Manual E-Way Bill data capture and status. - E-Way Bill API
adapter. - Transporter, vehicle, Part A/Part B data. -
Cancellation/update/extension workflows where permitted. - E-Invoice
eligibility checks. - IRN generation/status/cancellation. - Signed QR
and signed data storage. Acceptance: - Sandbox provider tests pass. -
Duplicate API submission is prevented or safely idempotent. - Provider
errors and retryable failures are distinguished. - External status is
reconciled before showing final state. - Legal rules and thresholds are
configuration/version controlled.

## MVP-7 -- Purchases and expenses

Deliver: - Purchase orders and purchase invoices. - Purchase
returns/adjustments. - Supplier payment and outstanding. - Expense
categories and expense entries. - Attachments. Acceptance: - Purchase
tax and totals reconcile. - Purchase posting updates stock only once. -
Expense records are reportable and auditable.

## MVP-8 -- Full inventory

Deliver: - Warehouses. - Stock ledger. - Stock transfers. - Adjustments
and physical counts. - Batch and expiry tracking. - Serial tracking
where enabled. - Barcode support. - FIFO and weighted average valuation
options. - Reserved/available/damaged stock. Acceptance: - Every stock
movement has a source and audit reference. - Stock balance reconciles
with movement ledger. - Concurrent sales cannot oversell unless an
explicit policy permits it. - Returns and cancellations reverse the
correct stock movements. - Historical cost and stock valuation are
reproducible.

## MVP-9 -- Dashboard and reports

Deliver: - Sales, purchase, expense, GST, outstanding, inventory
dashboards. - Report filters. - PDF/Excel exports. - E-Way
Bill/E-Invoice status reports. Acceptance: - Reports reconcile to
transactional data. - Date filters and timezone handling are correct. -
Large reports are paginated or generated asynchronously.

## MVP-10 -- Backup, restore and disaster recovery

Deliver: - PostgreSQL base backup and WAL archiving. - Local D
drive/external drive targets. - Cloud object storage provider. -
Encryption and integrity manifests. - Retention policy. - Restore wizard
and point-in-time recovery. - Backup status/reminders. Acceptance: -
Backup is not marked verified until checksum and restore test criteria
pass. - Restore is tested in an isolated database. - Restore requires
explicit confirmation and safety backup. - Missing D drive/cloud
failures are visible and recoverable.

## MVP-11 -- Notifications, audit and production hardening

Deliver: - Email and WhatsApp provider interfaces. - Reminder
schedules. - Audit logs. - Login history. - Diagnostics and support
export. - Installer upgrade/rollback strategy. - Performance and
security testing. Acceptance: - Delivery status reflects provider
response. - Sensitive data is redacted from logs. - Upgrade from each
supported previous version passes. - All P0 release gates pass.

## MVP-12 -- Advanced/future capabilities

-   POS and thermal printing.
-   Customer portal and secure quotation approval.
-   AI purchase-bill OCR.
-   Advanced analytics.
-   Multi-PC LAN mode.
-   Multi-branch.
-   Multi-company/cloud SaaS.
-   Full double-entry accounting.
-   Payroll/manufacturing only if separately approved.

## Backlog item format

Every ticket must include: - ID and MVP. - User story. - Business
rationale. - Preconditions. - Acceptance criteria. - Validation rules. -
API/UI/database impact. - Migration impact. - Security/privacy impact. -
Unit/integration/E2E tests. - Regression tests to run. - Documentation
updates.


## Executable delivery milestone attached to every MVP

A runnable Windows development/testing package is a deliverable from MVP-0 onward, not a final-phase task. Every MVP acceptance must include a refreshed package built from the real Angular + ASP.NET Core + WPF/WebView2 application, database migrations, a repeatable build script, smoke/regression test results, and `README-TESTING.md`.

The package may rely on a configured PostgreSQL test instance during early milestones. It must not be represented as the final customer installer. The final customer installer remains a post-MVP release deliverable after production hardening and release acceptance.
