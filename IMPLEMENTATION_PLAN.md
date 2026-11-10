# BillMitra – Development Execution & Implementation Plan

**Product:** BillMitra – Windows GST Billing, Inventory, and Business Management Software  
**Version:** 1.0  
**Architectural Baseline:** Modular Monolith (.NET 10 + Angular 22 + PostgreSQL + Dapper + WPF/WebView2)  
**Delivery Mandate:** Continuous Windows executable package from MVP-0 onwards (`artifacts/development/`).

---

## 1. Project Organization & Directory Structure

To fulfill all requirements, the solution will be structured cleanly under `d:\manektech\ResearchAndDevelopment\BillMitra`:

```text
BillMitra/
├── Antigravity_Billing_Blueprint/    # Original specification blueprints
├── artifacts/                         # Generated executable builds and packages
│   └── development/                   # Daily / MVP runnable testing builds
│       ├── BillMitra.exe              # Desktop Shell executable
│       ├── Backend/                   # Published ASP.NET Core API binaries
│       ├── Frontend/                  # Compiled Angular production SPA
│       ├── Config/                    # Runtime application settings & environment
│       ├── version.json               # Package & schema metadata
│       └── README-TESTING.md          # Launch instructions & verification checklist
├── database/
│   ├── migrations/                    # EF Core migration snapshots & SQL scripts
│   └── seed/                          # Default seed data (HSN master, default roles)
├── scripts/
│   ├── build-development.ps1          # Automated full-stack build & packaging script
│   ├── run-tests.ps1                  # Regression & test runner
│   └── start-dev-env.ps1              # Local dev services bootstrapper (PostgreSQL)
├── src/
│   ├── Desktop/
│   │   └── BillMitra.Desktop/         # WPF .NET 10 + WebView2 Shell project
│   ├── Frontend/
│   │   └── billmitra-ui/              # Angular 22 standalone app (PrimeNG + Signals)
│   ├── Backend/
│   │   ├── BillMitra.Domain/          # Pure entities, domain models, value objects, rules
│   │   ├── BillMitra.Application/     # CQRS/Services, business logic, DTOs, interfaces
│   │   ├── BillMitra.Infrastructure/  # Dapper repos, PostgreSQL, QuestPDF, ClosedXML
│   │   ├── BillMitra.Api/             # ASP.NET Core .NET 10 Web API, Kestrel, endpoints
│   │   └── BillMitra.Worker/          # Background worker service (Windows service host)
│   └── Shared/
│       └── BillMitra.Contracts/       # Shared DTO contracts, enums, API request/response
├── tests/
│   ├── BillMitra.UnitTests/           # Domain & financial calculation unit tests
│   ├── BillMitra.IntegrationTests/    # Dapper & PostgreSQL repository tests
│   ├── BillMitra.ApiTests/            # WebApplicationFactory API contract tests
│   └── BillMitra.ArchitectureTests/   # NetArchTest architectural boundary validation
├── BillMitra.sln                      # Master Visual Studio / .NET 10 Solution
├── PROJECT_AUDIT.md
├── IMPLEMENTATION_PLAN.md
├── TECHNICAL_RISKS.md
├── OPEN_QUESTIONS.md
└── PROJECT_STATUS.md
```

---

## 2. Executable Delivery Strategy ("Executable from Day 1")

### Automated PowerShell Packaging (`scripts/build-development.ps1`)
1. **Frontend Build**: `npm run build -- --configuration production` in `src/Frontend/billmitra-ui/`. Output goes to `dist/billmitra-ui/browser/`.
2. **Backend Publish**: `dotnet publish src/Backend/BillMitra.Api/BillMitra.Api.csproj -c Release -r win-x64 --self-contained false -o artifacts/development/Backend/`.
3. **Desktop Shell Build**: `dotnet publish src/Desktop/BillMitra.Desktop/BillMitra.Desktop.csproj -c Release -r win-x64 --self-contained false -o artifacts/development/`.
4. **Asset Integration**: Copy Angular `dist` assets into `artifacts/development/Frontend/` (or embedded in Backend wwwroot).
5. **Configuration & Versioning**: Write `version.json` containing Git hash, build date, semantic version, and database schema version.
6. **Launch & Verification Guide**: Generate `README-TESTING.md` detailing database prerequisites, test credentials, and smoke test steps.
7. **Execution**: The user simply double-clicks `artifacts/development/BillMitra.exe`. The shell starts the local Kestrel backend on loopback, validates health, and renders the Angular UI inside WebView2.

---

## 3. Phased MVP Roadmap & Backlog Breakdown

### MVP-0: Engineering Foundation & Day-1 Runnable Executable
- **Goal**: Scaffold complete solution, establish clean architecture boundaries, configure Dapper + EF Core migrations, setup Angular 22 + PrimeNG, build WPF shell, and produce the first runnable package.
- **Backend Tasks**:
  - Setup `.sln` and all .NET 10 projects (`Domain`, `Application`, `Infrastructure`, `Api`, `Worker`, `Contracts`).
  - Configure Serilog structured logging and ASP.NET Core Health Checks (`/health`, `/health/ready`).
  - Configure Dapper connection factory targeting PostgreSQL.
  - Setup EF Core DbContext strictly for schema migrations.
  - Implement global exception handling, standardized API response envelop (`ApiResponse<T>`), and OpenAPI/Swagger.
- **Frontend Tasks**:
  - Initialize Angular 22 project with standalone components, Signals, and PrimeNG.
  - Create responsive desktop application shell layout (collapsible sidebar, header with health status, theme baseline).
  - Implement base HTTP client interceptors with error handling and environment configuration.
- **Desktop Shell Tasks**:
  - Implement WPF application with `Microsoft.Web.WebView2`.
  - Add backend process supervisor: auto-spawns `BillMitra.Api` on loopback port if service is not running, monitors health check, and graceful shutdown on shell exit.
- **Database Tasks**:
  - Create initial migration baseline: `migration_history` and system health status table.
- **Packaging & Delivery Tasks**:
  - Create `scripts/build-development.ps1` that builds and compiles the solution into `artifacts/development/`.
  - Create `README-TESTING.md`.
- **Testing Tasks**:
  - Architecture tests (`BillMitra.ArchitectureTests`) ensuring no circular dependencies or direct controller-to-database calls.
  - API smoke tests (`BillMitra.ApiTests`).
- **Acceptance Deliverable**: Runnable `BillMitra.exe` in `artifacts/development/` that boots the desktop window, starts backend, connects to PostgreSQL, and renders the Angular application verifying system health.

---

### MVP-1: Windows Shell, Installer & First-Run Setup
- **Goal**: Implement first-run company setup wizard, owner account creation, and system configuration persistence.
- **Backend Tasks**:
  - Endpoints for setup state check (`/api/v1/setup/status`), company initialization, and initial owner credentials.
  - Password hashing using modern PBKDF2 / ASP.NET Core Identity PasswordHasher.
  - Windows Service integration mode for `BillMitra.Worker` / `BillMitra.Api`.
- **Frontend Tasks**:
  - First-run Setup Wizard component (Company name, GSTIN, Trade name, Address, Financial year start, Admin setup).
  - Client-side validation of GSTIN format, PAN extraction, and state code derivation.
- **Database Tasks**:
  - Tables: `company_profile`, `financial_years`, `users`, `roles`, `permissions`, `app_settings`.
- **Acceptance Deliverable**: Updated development build where a first-time launch displays the Setup Wizard and transitions to the main dashboard upon successful company initialization.

---

### MVP-2: Company, Users, Products & Customers (Masters)
- **Goal**: Complete master catalog management with Excel import/export.
- **Backend Tasks**:
  - Product & Service catalog CRUD, Units, Categories, HSN/SAC master.
  - Customer & Supplier masters with multiple billing/shipping addresses and GSTIN verification regex.
  - ClosedXML Excel import/export services with row-by-row validation error reports.
- **Frontend Tasks**:
  - Master screens in PrimeNG: DataTables with virtual scrolling, server-side search, filtering, and sort.
  - Excel upload dialog with preview and validation failure summary.
- **Database Tasks**:
  - Tables: `products`, `product_categories`, `units`, `hsn_sac_codes`, `customers`, `customer_addresses`, `suppliers`, `supplier_addresses`.
- **Acceptance Deliverable**: Updated executable supporting full catalog and party management with spreadsheet bulk imports.

---

### MVP-3: GST Calculation & Sales Documents
- **Goal**: Central GST calculation engine, atomic document numbering, sales document lifecycle, and QuestPDF rendering.
- **Backend Tasks**:
  - Central `GstCalculationService`: intra-state (CGST + SGST), inter-state (IGST), reverse charge, item-level discounts, rounding.
  - Atomic document numbering engine utilizing PostgreSQL row locks (`SELECT next_val FROM document_series WHERE ... FOR UPDATE`).
  - Document lifecycle transitions: Draft -> Finalized (immutable snapshot) -> Cancelled.
  - QuestPDF template for tax invoices and delivery challans.
- **Frontend Tasks**:
  - Fast, keyboard-first billing screen: product search, rate auto-fill, tax split display, live grand total calculation.
  - Invoice preview modal and direct print / PDF download.
- **Database Tasks**:
  - Tables: `document_series`, `sales_documents`, `sales_document_items`, `sales_document_charges`, `document_status_history`.
- **Acceptance Deliverable**: Executable capable of generating compliant GST invoices, previewing PDFs, and enforcing atomic serial numbers.

---

### MVP-4: Payments, Ledgers & Outstanding
- **Goal**: Receipts, payment allocations, customer/supplier ledgers, and overdue aging.
- **Backend Tasks**:
  - Transactional payment allocation engine supporting full, partial, and advance payments.
  - Ledger generation reconciling invoices, adjustments, and receipts.
  - Aging calculation (0-30, 31-60, 61-90, 90+ days).
- **Frontend Tasks**:
  - Record Payment dialog with multi-invoice allocation table.
  - Customer ledger statement view with date range filter and PDF/Excel export.
  - Outstanding aging summary dashboard.
- **Database Tasks**:
  - Tables: `payments`, `payment_allocations`, `payment_reversals`, `payment_modes`.
- **Acceptance Deliverable**: Executable with complete accounts receivable/payable tracking and allocation ledger.

---

### MVP-5: GST Reporting & Submission Foundation
- **Goal**: GSTR-1 preparation and JSON export, GSTR-3B summary, HSN register, and provider adapter foundation.
- **Backend Tasks**:
  - Aggregation engine for GSTR-1 tables: B2B, B2CL, B2CS, CDNR, EXP, HSN summary, Document summary.
  - Official government portal JSON export format generation.
  - `IGstReturnProvider` interface and mock/sandbox adapter.
- **Frontend Tasks**:
  - GST Returns dashboard: monthly/quarterly period selector, preparation status, reconciliation warnings, JSON download.
- **Database Tasks**:
  - Tables: `gst_return_periods`, `gst_return_snapshots`, `gst_submission_attempts`.
- **Acceptance Deliverable**: Executable producing offline GSTR-1 JSON files ready for upload to the GST portal.

---

### MVP-6: E-Way Bill & E-Invoice Integrations
- **Goal**: Part A/Part B data capture, E-Way Bill generation, IRN generation, and signed QR code display.
- **Backend Tasks**:
  - E-Way Bill lifecycle: manual entry, API integration (`IEWayBillProvider`), transporter/vehicle details.
  - E-Invoice lifecycle: eligibility check, IRP integration (`IEInvoiceProvider`), IRN storage, signed QR code rendering on QuestPDF invoices.
- **Frontend Tasks**:
  - E-Way Bill modal with vehicle/transporter fields.
  - E-Invoice action button on finalized invoices with IRN and QR preview.
- **Database Tasks**:
  - Tables: `eway_bills`, `einvoices`, `external_api_events`.
- **Acceptance Deliverable**: Executable supporting manual E-Way bill recording and sandbox API generation with signed QR code rendering.

---

### MVP-7: Purchases & Expenses
- **Goal**: Inward supplies, purchase bills, purchase returns, expense tracking, and input tax credit (ITC) recording.
- **Backend Tasks**:
  - Purchase bill entry with ITC eligibility classification.
  - Expense recording with category breakdowns and attachments.
- **Frontend Tasks**:
  - Purchase bill form and expense entry modal.
- **Database Tasks**:
  - Tables: `purchase_documents`, `purchase_document_items`, `expenses`, `expense_categories`.
- **Acceptance Deliverable**: Executable tracking outward and inward transactions and operating expenses.

---

### MVP-8: Full Inventory & Stock Ledger
- **Goal**: Multi-warehouse stock tracking, batch/expiry management, serial numbers, stock adjustments, and valuation (FIFO & Weighted Average).
- **Backend Tasks**:
  - Immutable stock transaction ledger (`stock_transactions`).
  - Stock valuation service (FIFO and Weighted Average).
  - Concurrency-safe inventory deduction on sales finalization.
- **Frontend Tasks**:
  - Stock dashboard, stock transfer wizard, physical count adjustment form, batch/expiry alerts.
- **Database Tasks**:
  - Tables: `warehouses`, `stock_transactions`, `stock_batches`, `stock_serials`, `stock_transfers`, `stock_counts`.
- **Acceptance Deliverable**: Executable with complete inventory controls preventing overselling and maintaining batch/serial traceability.

---

### MVP-9: Dashboards & Business Analytics
- **Goal**: Consolidated executive dashboard, drill-down reports, and scheduled summaries.
- **Backend Tasks**:
  - High-performance Dapper aggregation queries for sales trends, top products, customer balances, and tax liabilities.
- **Frontend Tasks**:
  - Modern dashboard with PrimeNG cards, KPI metrics, chart trends, and fast shortcuts.
- **Acceptance Deliverable**: Executable featuring an interactive, actionable business intelligence dashboard.

---

### MVP-10: Automated Backup, Restore & Point-in-Time Recovery
- **Goal**: PostgreSQL pg_dump / WAL archiving, local secondary drive backup, cloud S3 provider adapter, and verified restore wizard.
- **Backend Tasks**:
  - Backup orchestrator (`IBackupStorageProvider`), AES-256 encryption, checksum manifest generation.
  - Restore pre-flight validator with safety snapshot creation.
- **Frontend Tasks**:
  - Backup & Restore settings screen with manual trigger, schedule configuration, and restore drill wizard.
- **Database Tasks**:
  - Tables: `backup_configurations`, `backup_runs`, `backup_artifacts`.
- **Acceptance Deliverable**: Executable with one-click verified backup and restore drill testing.

---

### MVP-11: Production Hardening, Notifications & Installer
- **Goal**: Inno Setup installer package, WhatsApp/Email notification adapters, audit log viewer, and release gates.
- **Tasks**:
  - Inno Setup installer script bundling PostgreSQL, .NET runtime, WebView2, and registering Windows Service.
  - Email/WhatsApp adapter implementation.
  - Final end-to-end regression testing and performance benchmarking.
- **Acceptance Deliverable**: Production-ready Windows Installer (`BillMitra_Setup_v1.0.exe`).

