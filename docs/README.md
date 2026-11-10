# BillMitra – Architecture, Statutory & Engineering Documentation

**Version:** 1.0  
**Product:** Windows GST Billing, Inventory & Business Management Platform  
**Audience:** Architects, Developers, QA, Tax Auditors, and Contributors  

---

## Overview

This directory (`/docs`) defines the core product scope, Clean Architecture guidelines, statutory GST compliance rules, database schemas, document numbering lifecycle, UI/UX design standards, and testing policies for BillMitra.

## Read first

1.  `00_MASTER_INSTRUCTIONS.md`
2.  `01_PRODUCT_REQUIREMENTS.md`
3.  `02_ARCHITECTURE_AND_CLOUD_READINESS.md`
4.  `03_MVP_ROADMAP_AND_BACKLOG.md`
5.  `04_INVOICE_NUMBERING_AND_DOCUMENT_LIFECYCLE.md`
6.  `05_GST_EWAYBILL_EINVOICE_INTEGRATIONS.md`
7.  `06_DATABASE_AND_DATA_INTEGRITY.md`
8.  `07_UI_UX_STANDARDS.md`
9.  `08_TESTING_AND_NON_BREAKING_CHANGE_POLICY.md`
10. `09_WINDOWS_INSTALLER_BACKUP_AND_UPDATES.md`
11. `10_DEFINITION_OF_DONE_AND_RELEASE_CHECKLIST.md`

## Fixed product decisions

-   Windows desktop-first distribution.
-   Single business per installation for the initial product.
-   Trading and service businesses.
-   Angular 22, ASP.NET Core .NET 10, PostgreSQL, Dapper.
-   EF Core migrations only; Dapper for runtime data access.
-   PrimeNG, Angular Signals, WebView2 desktop shell.
-   Kestrel hosted as a Windows Service; IIS is not required.
-   Local-first operation; internet required only for online
    integrations, cloud backup and updates.
-   Local and cloud backup.
-   Full inventory capability.
-   GST report preparation/export plus direct submission through
    authorized integration providers where available.
-   Manual and API-driven E-Way Bill workflows.
-   E-Invoice/IRP integration for eligible taxpayers and documents.

## Change control

These documents are the baseline. If a requirement changes, update the
relevant document, add a decision record, update tests and identify
migration/compatibility impact before implementation.


## Executable delivery approach

The project is designed to produce a runnable Windows development/testing executable from MVP-0 and refresh it after every accepted MVP. The final customer installer is intentionally separate and is created only after full release acceptance. See `GEMINI.md`, `00_MASTER_INSTRUCTIONS.md`, and `09_WINDOWS_INSTALLER_BACKUP_AND_UPDATES.md` for the mandatory workflow.
