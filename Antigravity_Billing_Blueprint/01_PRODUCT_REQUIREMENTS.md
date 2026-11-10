# 01 -- Product Requirements Specification

## Product vision

A modern, easy-to-use Windows desktop application for Indian small and
medium businesses to manage GST billing, quotations, proforma invoices,
credit/debit notes, purchases, payments, outstanding, full inventory,
GST returns, E-Way Bills, E-Invoices, reports and backups.

The user is assumed to be non-technical. Installation, database setup,
updates and backups must be guided and automated.

## Target users

-   Retailers and wholesalers.
-   Traders and distributors.
-   Service providers.
-   Small manufacturers with basic inventory needs.
-   Business owners, billing operators, accountants and inventory
    operators.

## Product constraints

-   Initial installation is single-business and single-PC.
-   Design data model and API so a future cloud or LAN deployment does
    not require business logic rewrite.
-   Core operations work offline.
-   Online integrations are optional and show clear connectivity/status
    indicators.
-   The first accounting scope is basic operational accounting: sales,
    purchases, receipts, payments, expenses, ledgers and outstanding. Do
    not claim full double-entry accounting, statutory financial
    statements or complete accounting compliance unless separately
    implemented and validated.

## Functional modules

1.  Company/business setup.
2.  User, role and permission management.
3.  Product and service catalogue.
4.  Customer management.
5.  Supplier management.
6.  Sales documents: tax invoice, bill of supply, quotation, proforma,
    credit note, debit note, delivery challan, sales order, sales
    return, receipt voucher.
7.  GST calculation and reports.
8.  GSTR-1/GSTR-3B preparation, export and authorized submission
    integration.
9.  GSTR-2B import/reconciliation support.
10. E-Way Bill manual entry, generation, status, cancellation, vehicle
    update and validity workflows, subject to rules/API access.
11. E-Invoice eligibility, IRN generation/status/cancellation and signed
    QR data through authorized IRP integration.
12. Payments, allocations, advances, receipts and reversals.
13. Customer/supplier ledger and outstanding aging.
14. Purchase management and purchase returns.
15. Expense management.
16. Full inventory: warehouses, transfers, stock ledger, batches,
    expiry, serials, barcode, valuation, adjustments and stock counts.
17. Dashboard and reports.
18. Reminders and notifications.
19. Backup, restore and recovery.
20. Installer, update and application health management.
21. Audit trail and document/file management.

## Global user experience

-   One primary action per page where practical.
-   Use business language, not developer terminology.
-   Keep common actions visible: Create Invoice, Add Customer, Add
    Product, Record Payment, Create Quotation.
-   Use clear empty states with a next action.
-   Show validation next to the relevant field.
-   Confirm irreversible actions and explain their impact.
-   Provide keyboard navigation for billing data entry.
-   Use searchable selectors for customers/products.
-   Keep document previews readable and print-friendly.
-   Avoid overcrowded dashboards and excessive nested navigation.

## Non-functional requirements

-   Reliable financial calculations and traceability.
-   Strong data integrity and transactional operations.
-   Responsive UI for typical desktop resolutions.
-   Secure local API and database.
-   No secrets in frontend bundles or logs.
-   Database backup and verified restore.
-   Clear error handling and support diagnostics.
-   Accessibility: keyboard operation, visible focus, readable contrast
    and labels.
-   Cloud portability: no business logic tied to local filesystem or
    Windows services.
-   Maintainability: modular code, tests, documented migrations and API
    contracts.

## Out of scope for initial release

-   Multi-tenant SaaS administration.
-   Full double-entry accounting/ledger engine.
-   Payroll.
-   Manufacturing BOM/MRP.
-   E-commerce storefront.
-   Native mobile application.
-   Multi-PC LAN deployment unless separately approved.
-   Automated bank feeds unless a provider is selected.

These may be future products or optional modules.
