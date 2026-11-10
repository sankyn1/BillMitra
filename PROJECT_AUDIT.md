# BillMitra – Documentation and Specification Audit Report

**Date:** November 10, 2026  
**Auditor:** Principal Software Architect & Technical Lead (Antigravity)  
**Product:** BillMitra – Windows GST Billing, Inventory & Business Management Software  
**Status:** Audit Completed  

---

## 1. Documentation Files Successfully Inspected and Audited

The entire documentation pack located in `Antigravity_Billing_Blueprint` was read and analyzed in its entirety:

| Document File | Purpose / Scope in Blueprint | Audit Status |
| :--- | :--- | :--- |
| `GEMINI.md` | Permanent project instructions, non-negotiable principles, executable from Day 1 mandate | Fully Read & Verified |
| `00_MASTER_INSTRUCTIONS.md` | Master engineering rules, tech stack decisions, multi-role workflow | Fully Read & Verified |
| `01_PRODUCT_REQUIREMENTS.md` | Target users, functional modules (1–21), boundaries, out-of-scope items | Fully Read & Verified |
| `02_ARCHITECTURE_AND_CLOUD_READINESS.md`| Modular monolith, layer separation, cloud portability rules, API conventions | Fully Read & Verified |
| `03_MVP_ROADMAP_AND_BACKLOG.md` | Cumulative roadmap (MVP-0 to MVP-12), backlog format, delivery rules | Fully Read & Verified |
| `04_INVOICE_NUMBERING_AND_DOCUMENT_LIFECYCLE.md` | Rule 46 compliance, sequence allocation, state machine, snapshots | Fully Read & Verified |
| `05_GST_EWAYBILL_EINVOICE_INTEGRATIONS.md` | Provider abstraction, GSTR workflows, E-Way Bill, E-Invoice/IRP | Fully Read & Verified |
| `06_DATABASE_AND_DATA_INTEGRITY.md` | PostgreSQL standards, Dapper runtime, EF migrations, table groups | Fully Read & Verified |
| `07_UI_UX_STANDARDS.md` | Visual design, PrimeNG + Signals, keyboard-first billing, responsive layout | Fully Read & Verified |
| `08_TESTING_AND_NON_BREAKING_CHANGE_POLICY.md` | Test pyramid, cumulative regression suites, financial test cases | Fully Read & Verified |
| `09_WINDOWS_INSTALLER_BACKUP_AND_UPDATES.md` | Local deployment, directory structure, PITR backup, Inno Setup/WiX | Fully Read & Verified |
| `10_DEFINITION_OF_DONE_AND_RELEASE_CHECKLIST.md`| Definition of Done, production release gates, executable criteria | Fully Read & Verified |
| `DECISIONS.md` | Initial ADRs (ADR-001 through ADR-007) | Fully Read & Verified |
| `PACKAGE_VERSION.txt` | Blueprint release metadata (Final v2) | Fully Read & Verified |
| `README.md` | Overview, reading order, and baseline principles | Fully Read & Verified |

---

## 2. Product Requirements Understood

1. **Target Market**: Indian Small and Medium Businesses (Retailers, Wholesalers, Distributors, Traders, Service Providers, and Small Manufacturers).
2. **Operational Posture**: Local-first, offline-first execution for core billing, inventory, and ledger. Internet connectivity is utilized strictly for external statutory integrations (GSP, IRP, E-Way Bill portal), cloud backups, and version updates.
3. **Tenancy Model**: Single business, single-PC installation initially. Clean domain abstractions guarantee zero code rewrite when multi-branch or cloud SaaS is introduced in later roadmaps.
4. **Scope Boundaries**:
   - *Included*: Full GST billing & documents (Tax Invoice, Bill of Supply, Quotation, Proforma, Credit/Debit Note, Delivery Challan), Basic Operational Accounting (Customer/Supplier Ledgers, Receivables, Payables, Aging, Expenses), Full Inventory (Multi-warehouse, Stock Ledger, Batch/Expiry, Serials, FIFO/Weighted Average valuation), GST Returns (GSTR-1, GSTR-3B summary, GSTR-2B reconciliation foundation), E-Way Bill & E-Invoice integration adapters, Automated Local/Cloud Backups.
   - *Excluded from Initial Scope*: Multi-tenant SaaS admin, full double-entry general ledger / balance sheet compliance, payroll, manufacturing MRP/BOM, e-commerce storefront, and mobile apps.

---

## 3. Architecture & Technical Invariants Confirmed

- **Architecture Style**: Modular Monolith organized with clean layered separation (`Domain`, `Application`, `Infrastructure`, `Api`, `Worker`, `Desktop`, `Contracts`).
- **Frontend**: Angular 22 with standalone components, Signals for state management, Reactive Forms for data entry, PrimeNG component suite, SCSS styling adhering to commercial desktop UX standards.
- **Backend**: ASP.NET Core (.NET 10) Web API running Kestrel, bound strictly to loopback (`127.0.0.1`) in single-PC mode.
- **Desktop Shell**: WPF (.NET 10) embedding `Microsoft.Web.WebView2` for native Windows integration, tray notifications, process lifecycle management, and offline shell delivery.
- **Database Access & Migrations**:
  - Runtime queries & commands: **Dapper** with strictly parameterized SQL.
  - Schema migrations & design-time tooling: **Entity Framework Core Migrations** (forward-only, additive migrations).
- **Financial Precision & Integrity**: PostgreSQL `NUMERIC` / .NET `decimal`. Document numbers allocated atomically using server-side sequence locking (`SELECT ... FOR UPDATE` on sequence rows). Finalized documents are completely immutable snapshots. Corrections happen via cancellations, credit notes, or debit notes.
- **Reporting & Document Rendering**: QuestPDF for pixel-perfect PDF rendering (Tax Invoices, Delivery Challans); ClosedXML for Excel reports and spreadsheet imports/exports.
- **External Provider Abstraction**: Dedicated interfaces (`IGstReturnProvider`, `IEWayBillProvider`, `IEInvoiceProvider`, `IBackupStorageProvider`, `IFileStorage`, `IEmailProvider`, `IWhatsAppProvider`) preventing vendor lock-in.

---

## 4. Technology Stack Confirmation

| Layer / Concern | Technology Selection | Version / Specification | Rationale & Status |
| :--- | :--- | :--- | :--- |
| **Desktop Shell** | WPF + WebView2 | .NET 10 / Microsoft.Web.WebView2 | Native Windows windowing, lifecycle control, hardware acceleration |
| **Frontend UI** | Angular | v22 (Standalone + Signals) | High performance, typed reactivity, maintainable modular structure |
| **UI Components** | PrimeNG | Latest compatible with Angular 22 | Rich data grids, dialogs, calendars, and enterprise controls |
| **Backend API** | ASP.NET Core | .NET 10 | Cross-platform, high throughput, robust hosting as Windows Service or process |
| **Database** | PostgreSQL | 16+ | ACID compliance, strong indexing, row-level locking, JSONB snapshots |
| **Runtime Data Access** | Dapper | 2.1+ | Micro-ORM speed, explicit parameterized SQL queries, minimal overhead |
| **Schema Migrations** | EF Core Migrations | .NET 10 | Versioned, auditable, reproducible schema changes without manual scripts |
| **PDF Engine** | QuestPDF | Community / Standard | Declarative, high-fidelity PDF invoice generation |
| **Excel Engine** | ClosedXML | Latest stable | Native OpenXML manipulation for reports and data bulk import |
| **Testing** | xUnit, FluentAssertions, Moq, Respawn, Playwright | Latest | Unit, integration, architecture, and E2E testing |

---

## 5. Identified Discrepancies, Ambiguities, and Missing Requirements

During the documentation audit, the following points requiring technical decisions or clarifications were identified:

### 5.1. Development Executable vs. Windows Service Hosting (Operational Conflict)
- *Blueprint Citation*: Document 02 & 09 dictate hosting Kestrel as a Windows Service (`Billing.Worker` or `Billing.Api`), while Document 00 & GEMINI.md mandate a runnable development executable from MVP-0 (`artifacts/development/BillingSoftware.exe`).
- *Analysis*: In a development and rapid-testing environment, creating or restarting a Windows Service requires elevated local Administrator privileges (`sc.exe create/start`), which causes friction and CI/dev machine permission issues.
- *Recommendation*:
  1. The Desktop Shell (`Billing.Desktop`) in development mode should launch the backend `Billing.Api` as a managed background child process if the Windows Service is not registered.
  2. For the final release installer (MVP-1 / MVP-11), the installer registers and starts the actual Windows Service (`Billing.Service`), and the Desktop Shell connects to the service via loopback health check.

### 5.2. Installer Technology Choice: Inno Setup vs. WiX Toolset
- *Blueprint Citation*: Document 00 and 09 state: *"Inno Setup or WiX for Windows installer; choose one and document why."*
- *Analysis*: 
  - WiX Toolset produces standard Windows Installer `.msi` packages but involves verbose XML maintenance and complex custom actions for silent PostgreSQL instance installation and migration execution.
  - Inno Setup provides Pascal scripting, rock-solid silent execution of bundled third-party installers (PostgreSQL setup, WebView2 bootstrapper), easy service registration, and simple registry/shortcut management.
- *Recommendation*: Choose **Inno Setup** as the official installer technology. Record this as ADR-008 in `DECISIONS.md` once approved.

### 5.3. PostgreSQL Instance Isolation on Customer Machine
- *Blueprint Citation*: Document 09 mandates that the installer must install a dedicated PostgreSQL instance and not overwrite or conflict with any existing customer PostgreSQL instance.
- *Analysis*: If a customer already runs PostgreSQL on port 5432 (e.g., for other software), installing or binding to 5432 will fail.
- *Recommendation*: The production installer should configure PostgreSQL on a dedicated custom port (e.g., `5433` or dynamically selected) and dedicated service name (`BillMitraPostgreSQL`), with an isolated data directory (`C:\ProgramData\BillMitra\Database`).

### 5.4. Composition Scheme vs. Regular Scheme GST Invoicing
- *Blueprint Citation*: Document 01 & 04 cover Tax Invoices and Bills of Supply, but the rules for Composition Scheme dealers require explicit constraint handling.
- *Analysis*: Composition taxpayers cannot issue a Tax Invoice, cannot charge CGST/SGST/IGST to customers, and must state "Composition taxable person, not eligible to collect tax on supplies" on their Bills of Supply.
- *Recommendation*: Enforce strict document creation rules based on `CompanyProfile.GstRegistrationType`. If `Composition`, disallow Tax Invoices and enforce Bill of Supply with zero tax lines.

### 5.5. Numbering Constraint Enforcement (CGST Rule 46)
- *Blueprint Citation*: Document 04 highlights the 16-character limit for GST invoice serial numbers.
- *Analysis*: If a user customizes a prefix (e.g., `MANEK/SURAT/INV/2026-2027/000001`), the resulting string will exceed 16 characters and violate Rule 46.
- *Recommendation*: Implement a strict configuration validator in the backend `DocumentSeriesService` that validates regex patterns `^[a-zA-Z0-9/-]+$` and dynamically checks `Prefix.Length + Token.Length + PaddingLength <= 16`.

---

## 6. Audit Conclusion
The documentation pack provided is thorough, highly structured, domain-accurate regarding Indian GST laws, and architecturally sound. With the clarifications and recommendations outlined above, the project is ready for structured implementation starting with MVP-0.

