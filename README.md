# BillMitra (बिलमित्र) ⚡

> **Commercial-Grade, Offline-First Windows GST Billing, Inventory & Business Management Platform**  
> *Engineered for Indian MSMEs, Traders, Retailers, Wholesalers & Service Providers.*

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular 22](https://img.shields.io/badge/Angular-22.0-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.8-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![WPF WebView2](https://img.shields.io/badge/Desktop-WPF%20%2B%20WebView2-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/microsoft-edge/webview2/)
[![GST Ready](https://img.shields.io/badge/GST-GSTR--1%20%7C%20GSTR--3B-008080?logo=indiatv&logoColor=white)](https://www.gst.gov.in/)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%20%7C%20SQL%20Server%20%7C%20SQLite-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT%20%2F%20Commercial-blue.svg)](#license)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/sankyn1/BillMitra/pulls)

---

## 🌟 Why BillMitra?

Most billing and accounting software for Indian small businesses suffers from either **sluggish legacy desktop architectures** or **unreliable cloud-only SaaS apps** that stop functioning the moment the internet connection drops at a retail checkout counter or warehouse.

**BillMitra** combines the best of both worlds:
- **100% Offline-First Desktop Reliability**: Runs locally on Windows with zero internet dependency for standard daily billing, inventory tracking, and payment processing.
- **Ultra-Modern 2026 Tech Stack**: Built with **.NET 10 (C# 13)**, **Angular 22 (Standalone + Signals)**, **PrimeNG**, and **Microsoft Edge WebView2**.
- **Commercial Competitor Parity**: Designed from the ground up to offer a faster, more flexible, and developer-friendly alternative to *TallyPrime*, *Busy Accounting*, *Zoho Books*, and *Vyapar*.
- **Comprehensive Indian GST Compliance**: Generates government-compliant GST Tax Invoices, Credit/Debit Notes, Table 12 HSN summaries, and official JSON exports ready for direct upload to the GST Common Portal.

---

## 🚀 Key Features & Capabilities

### 🧾 1. Statutory GST Invoicing & Sales Lifecycle
* **Multi-Format Billing**: Create standard **Tax Invoices**, **Proforma Invoices**, **Quotations / Estimates**, **Delivery Challans**, **Credit Notes**, and **Debit Notes**.
* **Statutory Classification**: Automatically classifies supplies into **B2B** (registered buyers with GSTIN), **B2CL** (inter-state unregistered supplies > ₹2,50,000 threshold), **B2CS** (intra-state and small inter-state), **SEZ with/without tax**, **Exports**, and **Reverse Charge Mechanism (RCM)**.
* **Central GST Calculation Engine**: Precision line-item calculations for Taxable Value, CGST, SGST, IGST, Cess, item discounts, cash discounts, and standard round-off algorithms.
* **Atomic Sequential Numbering**: Guarantees strictly gap-free, audit-proof sequence numbers per financial year (e.g., `INV/26-27/0001`, `PI/26-27/0001`, `CN/26-27/0001`).

### 🔄 2. One-Click Proforma & Quotation Conversion
* **Seamless Document Workflow**: Benchmarked against Zoho Books and TallyPrime. Convert approved Quotations (`QT/...`) and Proforma Invoices (`PI/...`) directly into official GST Tax Invoices with a single click.
* **Commercial Audit Linkage**: Converted tax invoices permanently reference their origin source document (e.g., `"Order/Ref: PI/26-27/0001"`).
* **Anti-Duplication Locks**: Converted documents transition to `DocumentStatus.Converted` and are protected against duplicate conversion attempts.

### 🖨️ 3. Print & Stationery Engine
* **Multiple Output Formats**:
  * **Standard A4**: Comprehensive business invoice with complete bank details, terms, and tax breakdown matrices.
  * **Compact A5 Landscape**: High-efficiency, ink-saving format for counter delivery.
  * **80mm POS Thermal Receipt**: Optimized for high-speed POS thermal slip printers at busy checkout counters.
* **Digital Letterhead vs. Pre-Printed Stationery**:
  * **Digital Letterhead**: Generates full company headers, logos, and statutory credentials.
  * **Pre-Printed Stationery Mode**: Suppresses the top company box and applies a configurable top margin gap (20mm–100mm) so invoices print directly onto pre-printed letterhead paper without overlapping.
  * **Custom Graphic Banner Upload**: Upload company header banners directly in settings.
* **In-App Interactive Preview & Direct Printing**: Instant PDF modal preview with format switcher, plus one-click **Direct Print** directly to the Windows Print Spooler.

### 📊 4. GST Returns (GSTR-1, GSTR-3B & Table 12 HSN)
* **GSTR-1 Outward Supply Preparation**:
  * **Table 4A**: B2B invoices grouped by customer GSTIN.
  * **Table 5A**: B2CL inter-state unregistered invoices (> ₹2.5L).
  * **Table 7**: B2CS supplies grouped by Place of Supply and tax rate.
  * **Table 9B**: Credit and Debit Notes (CDNR & CDNUR).
  * **Table 12**: HSN/SAC summary aggregating quantities, values, and tax splits.
  * **Table 13**: Sequential summary of all documents issued and cancelled during the period.
* **Official GST Portal JSON Export**: One-click download of `GSTR1_{GSTIN}_{Period}.json` validated against official GSTN offline schemas for direct upload to `gst.gov.in`.
* **GSTR-3B Return Matrix**: Automated calculation of Table 3.1 outward taxable & RCM supplies, Table 3.2 inter-state unregistered supplies, and Table 4 eligible ITC.
* **GSP Sandbox Filing Tracker**: Provider-agnostic submission interface (`IGstReturnProvider`) with sandbox simulator generating GSTN-compliant 15-character ARNs and persistent filing audit history.

### 💰 5. Payments, Receipts & Aging Ledgers
* **Payment Processing**: Record incoming customer payments and supplier payouts with automatic voucher numbering (`RV/26-27/0001`, `PV/26-27/0001`).
* **Multi-Invoice FIFO Allocation**: Smart allocation algorithm applying receipts across multiple outstanding invoices.
* **Customer & Supplier Statements**: Real-time debit/credit running balances and ledger exports.
* **Accounts Receivable & Aging Analysis**: Automated aging buckets (0–30 days, 31–60 days, 61–90 days, 90+ days overdue).

### 📦 6. Master Catalogs & Hybrid Storage Architecture
* **Product & Service Catalog**: Manage items with SKU, barcode, HSN/SAC codes, primary & secondary units, standard GST tax rates, and purchase/selling prices.
* **Party Directory**: Customers and suppliers with GSTIN validation, state codes, PAN, credit limits, and billing/shipping addresses.
* **Zero-Config Database Flexibility**:
  * **SQLite**: Embedded zero-configuration engine (ideal for single-PC standalone setups).
  * **Microsoft SQL Server / SQL Express**: Enterprise on-premises installation for multi-terminal local networks.
  * **PostgreSQL**: Production-grade relational database for robust high-volume operations.

---

## 🏗️ Architecture & Technology Stack

BillMitra is built following strict **Clean Architecture** and **Domain-Driven Design (DDD)** principles:

```
┌─────────────────────────────────────────────────────────────┐
│                 BillMitra.Desktop (WPF)                     │
│      Microsoft Edge WebView2 Desktop Supervised Shell       │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│             BillMitra UI (Angular 22 + PrimeNG)             │
│        Standalone Components, Angular Signals, RxJS         │
└──────────────────────────────┬──────────────────────────────┘
                               │ HTTP / REST API (Localhost)
┌──────────────────────────────▼──────────────────────────────┐
│              BillMitra.Api (.NET 10 Web API)                │
│       Controllers, Security, Local Windows Service Host      │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│           BillMitra.Application (Use Cases & CQRS)          │
│       Business Rules, Document Workflows, GST Engine        │
└───────────────┬──────────────────────────────┬──────────────┘
                │                              │
┌───────────────▼──────────────┐┌──────────────▼──────────────┐
│   BillMitra.Domain (Core)    ││ BillMitra.Infrastructure    │
│  Entities, Aggregates, Enums ││ Dapper, DbConnectionFactory │
│   Immutable Domain Logic     ││ QuestPDF, GSP Adapters      │
└──────────────────────────────┘└──────────────────────────────┘
```

| Layer | Technologies Used |
| :--- | :--- |
| **Desktop Shell** | WPF, C# 13, Microsoft Edge WebView2, Process Supervisor |
| **Frontend UI** | Angular 22, TypeScript 5.8, PrimeNG 19, Tailwind CSS, PrimeIcons |
| **Backend API** | ASP.NET Core (.NET 10), Kestrel, FluentValidation, Serilog |
| **Data Access** | Dapper (High-Performance Runtime SQL), EF Core (Schema Migrations) |
| **Supported Databases** | Microsoft SQL Server / SQL Express, PostgreSQL, SQLite |
| **Document Generation** | QuestPDF (A4, A5, 80mm POS Thermal, Vector Graphics) |
| **Test Engineering** | xUnit, FluentAssertions, Moq, Respawn, NetArchTest |

---

## 📁 Repository Structure

```
BillMitra/
├── src/
│   ├── Backend/
│   │   ├── BillMitra.Domain/          # Core entities, value objects, GST enums
│   │   ├── BillMitra.Application/     # Services, interfaces, validation, GST calculator
│   │   ├── BillMitra.Infrastructure/  # Dapper repositories, DB initializers, PDF generator
│   │   ├── BillMitra.Api/             # ASP.NET Core Web API, controllers, middleware
│   │   └── BillMitra.Worker/          # Background worker service (sync & backups)
│   ├── Desktop/
│   │   └── BillMitra.Desktop/         # WPF Windows host shell with WebView2 integration
│   ├── Frontend/
│   │   └── billmitra-ui/              # Angular 22 standalone app with modern design system
│   └── Shared/
│       └── BillMitra.Contracts/       # Shared DTOs, API contracts, JSON models
├── tests/
│   ├── BillMitra.UnitTests/           # Unit tests (GST rules, conversion, validation)
│   ├── BillMitra.ArchitectureTests/   # Clean Architecture & dependency enforcement tests
│   ├── BillMitra.IntegrationTests/    # Database connection & repository tests
│   └── BillMitra.ApiTests/            # Full-stack integration tests for HTTP endpoints
├── docs/                              # Formal architectural blueprints & statutory specs
├── scripts/                           # Build, package, and test automation scripts
└── artifacts/                         # Generated development executables and distributions
```

---

## ⚡ Quick Start & Development Setup

### Prerequisites
* Windows 10/11 (64-bit)
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js 22+ LTS](https://nodejs.org/) & npm
* Visual Studio 2026 / Visual Studio Code / JetBrains Rider

### 1. Clone Repository
```powershell
git clone https://github.com/sankyn1/BillMitra.git
cd BillMitra
```

### 2. Run Automated Test Suite
To verify your environment and validate all unit, architecture, and integration tests:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-tests.ps1
```

### 3. Build Runnable Development Executable
To compile the Angular frontend, publish the backend, and package the standalone Windows executable:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-development.ps1
```
The compiled, runnable executable will be located at:
```
artifacts/development/BillMitra.exe
```

### 4. Running in Live Development Mode (Hot Reload)
To run frontend and backend separately for day-to-day coding:

* **Backend API**:
  ```powershell
  cd src/Backend/BillMitra.Api
  dotnet run
  ```
  API Swagger will be live at `http://localhost:5000/swagger`.

* **Frontend UI**:
  ```powershell
  cd src/Frontend/billmitra-ui
  npm install
  npm start
  ```
  Web interface will be live at `http://localhost:4200`.

---

## 🗺️ Milestone Roadmap & Progress

| Milestone | Deliverable | Scope | Status | Deliverable |
| :---: | :--- | :--- | :---: | :--- |
| **MVP-0** | **Engineering Foundation** | Solution scaffolding, Angular 22, WPF Shell, Dapper, Tests | ✅ Complete | Build Package |
| **MVP-1** | **Windows Shell & Setup** | First-run wizard, company profile initialization, secure credentials | ✅ Complete | v0.1.0-beta |
| **MVP-2** | **Masters & Catalog** | Product catalog, categories, HSN/SAC directory, parties | ✅ Complete | v0.3.0-beta |
| **MVP-3** | **GST Invoicing Engine** | Central GST calculator, gap-free numbering, QuestPDF generation | ✅ Complete | v0.4.0-beta |
| **MVP-4** | **Payments & Ledgers** | Receipts, multi-invoice allocations, party ledgers, aging analysis | ✅ Complete | v0.5.0-beta |
| **MVP-4.1**| **Conversion & Stationery** | Proforma to Tax Invoice conversion, print preview, direct print, letterhead | ✅ Complete | v0.5.1-beta |
| **MVP-5** | **GST Returns (GSTR-1/3B)** | GSTR-1 JSON export, GSTR-3B summary, Table 12 HSN, sandbox GSP filing | ✅ Complete | v0.6.0-beta |
| **MVP-6** | **E-Way Bill & E-Invoice** | E-Way Bill generation, IRN generation, signed QR code, NIC/IRP sandbox | 🚀 Next Up | v0.7.0-beta |
| **MVP-7** | **Purchases & Expenses** | Inward purchase invoices, ITC eligibility, expense tracker | ⏳ Scheduled | v0.8.0-beta |
| **MVP-8** | **Full Inventory** | Stock ledger, multi-warehouse, batch/expiry, FIFO valuation | ⏳ Scheduled | v0.9.0-beta |
| **MVP-9** | **Executive Dashboards** | Revenue metrics, receivables, sales trends, inventory health | ⏳ Scheduled | v0.10.0-beta |
| **MVP-10**| **Backup & Disaster Recovery** | Automated local WAL backups, cloud sync, 1-click restore | ⏳ Scheduled | v0.11.0-beta |
| **MVP-11**| **Production Installer** | Inno Setup Windows installer, auto-updater, release gate | ⏳ Scheduled | v1.0.0 GA |

---

## 📚 Technical Documentation

Comprehensive statutory and architectural documentation is available in the [`docs/`](./docs) directory:
* [`01_PRODUCT_REQUIREMENTS.md`](./docs/01_PRODUCT_REQUIREMENTS.md) — Detailed functional requirements.
* [`02_ARCHITECTURE_AND_CLOUD_READINESS.md`](./docs/02_ARCHITECTURE_AND_CLOUD_READINESS.md) — Architectural principles and cloud hybrid design.
* [`03_MVP_ROADMAP_AND_BACKLOG.md`](./docs/03_MVP_ROADMAP_AND_BACKLOG.md) — Complete milestone scope breakdown.
* [`04_INVOICE_NUMBERING_AND_DOCUMENT_LIFECYCLE.md`](./docs/04_INVOICE_NUMBERING_AND_DOCUMENT_LIFECYCLE.md) — Statutory invoice numbering policies.
* [`05_GST_EWAYBILL_EINVOICE_INTEGRATIONS.md`](./docs/05_GST_EWAYBILL_EINVOICE_INTEGRATIONS.md) — GST, E-Way Bill, and E-Invoice statutory requirements.
* [`06_DATABASE_AND_DATA_INTEGRITY.md`](./docs/06_DATABASE_AND_DATA_INTEGRITY.md) — Relational schema, indexes, and concurrency.
* [`07_UI_UX_STANDARDS.md`](./docs/07_UI_UX_STANDARDS.md) — Design system standards, keyboard navigation, and theme rules.
* [`08_TESTING_AND_NON_BREAKING_CHANGE_POLICY.md`](./docs/08_TESTING_AND_NON_BREAKING_CHANGE_POLICY.md) — Non-breaking change policies and test requirements.

---

## 🤝 Contributing

Contributions, feedback, and issue reports are warmly welcomed!
1. Fork the Project.
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3. Commit your Changes (`git commit -m 'feat: Add AmazingFeature'`).
4. Push to the Branch (`git push origin feature/AmazingFeature`).
5. Open a Pull Request.

---

## ⭐ Star the Repository

If you find BillMitra helpful, interesting, or a valuable reference for modern .NET + Angular offline-first desktop applications, please **give this repository a star on GitHub**! It motivates the team and helps other developers and MSMEs discover the project.

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.
Copyright © 2026 BillMitra Contributors.
