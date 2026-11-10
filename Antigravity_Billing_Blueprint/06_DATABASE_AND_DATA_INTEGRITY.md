# 06 -- Database and Data Integrity Standards

## General

-   PostgreSQL is the system of record.
-   Dapper is used for runtime data access.
-   EF Core is used only for migrations.
-   UUID primary keys are preferred for portable IDs.
-   Use `NUMERIC(p,s)` for all financial amounts.
-   Use .NET `decimal`; never float/double for money.
-   Store timestamps in UTC (`timestamptz`).
-   Store currency code explicitly, initially INR.
-   Use foreign keys, check constraints and unique indexes.
-   Use parameterized SQL.
-   Use explicit transactions for multi-table financial/stock
    operations.

## Core table groups

Company/config: - company_profile - financial_years - document_series -
app_settings - payment_terms - tax_rate_versions

Security: - users - roles - permissions - user_roles -
role_permissions - user_sessions - audit_logs

Masters: - customers - customer_addresses - suppliers -
supplier_addresses - products - product_categories - units -
hsn_sac_codes - product_price_history

Sales: - sales_documents - sales_document_items -
sales_document_charges - document_status_history - document_references -
document_attachments

Purchases/expenses: - purchase_documents - purchase_document_items -
purchase_returns - expenses - expense_categories - expense_attachments

Payments: - payments - payment_allocations - payment_reversals -
payment_modes

Inventory: - warehouses - stock_transactions - stock_balances
(derived/cache, reconciled against ledger) - stock_reservations -
stock_batches - stock_serials - stock_counts - stock_transfers

GST/integrations: - gst_return_periods - gst_return_snapshots -
gst_submission_attempts - gst_reconciliations - eway_bills - einvoices -
external_api_events

Notifications/files/backups: - notification_templates -
notification_jobs - notification_history - stored_documents -
backup_configurations - backup_runs - backup_artifacts

## Financial document model

Use a shared document header and line model where it is appropriate,
with a typed document category and explicit lifecycle. Avoid one giant
generic table with dozens of nullable columns. Use extension tables for
document-specific fields such as transport details, IRN and E-Way Bill
metadata.

## Snapshot requirements

Finalized documents must snapshot relevant customer, company, item, tax
and address values. Master-data edits must not rewrite historical
documents.

## Ledger and outstanding

Use immutable source transactions and payment allocations. Outstanding
should be derived/reconciled from: - Finalized document totals. -
Applicable credit/debit adjustments. - Payment allocations. - Valid
reversals/cancellations.

Do not maintain a manually editable outstanding field as the only source
of truth.

## Inventory

Stock movement is a ledger. Each movement must include: - Product. -
Warehouse. - Quantity and unit. - Movement type. - Source
document/reference. - Batch/serial when applicable. - Effective
timestamp. - Actor. - Reversal reference where applicable.

Stock balances may be cached for speed but must be rebuildable from the
stock ledger.

## Migration policy

-   Migrations are forward-only by default.
-   Prefer additive changes: new nullable columns, new tables, indexes,
    backfills.
-   For renames/type changes use expand-migrate-contract:
    1.  Add new structure.
    2.  Deploy code that can read/write compatible old and new
        structures.
    3.  Backfill and verify.
    4.  Switch reads.
    5.  Remove old structure only in a later approved release.
-   Never drop/rename a populated column in the same release without a
    compatibility plan.
-   Every migration tested against empty DB and previous supported
    release DB.
-   Keep a migration ledger and backup before production migration.

## Indexing

Add indexes for document number, document date, customer/supplier IDs,
status, product IDs, stock references, payment references, financial
year and common report filters. Validate indexes with realistic query
plans.
