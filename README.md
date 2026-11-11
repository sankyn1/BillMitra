# BillMitra (बिलमित्र) ⚡

> **Commercial-Grade, 100% Offline-First Windows GST Billing, Invoicing & Inventory Software**  
> *A modern, lightning-fast alternative to Tally and Vyapar for Indian businesses.*

[![Website](https://img.shields.io/badge/Website-sankyn1.github.io%2FBillMitra-2563EB?logo=googlechrome&logoColor=white)](https://sankyn1.github.io/BillMitra/)
[![Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D7?logo=windows&logoColor=white)](https://sankyn1.github.io/BillMitra/#download)
[![GST Ready](https://img.shields.io/badge/GST-GSTR--1%20%7C%20GSTR--3B-008080?logo=indiatv&logoColor=white)](https://www.gst.gov.in/)
[![Offline First](https://img.shields.io/badge/Operation-100%25%20Offline-success)](https://sankyn1.github.io/BillMitra/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](#license)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/sankyn1/BillMitra/pulls)

---

## 📖 What is BillMitra?

**BillMitra (बिलमित्र)** is a complete, ready-to-use desktop billing, inventory, and GST management software designed specifically for Indian retail shops, wholesale traders, distributors, and service providers. 

It runs **100% locally on your Windows PC**. You do **not** need an active internet connection to generate invoices, print thermal receipts, or track customer balances. Your financial data stays entirely on your computer with **zero mandatory recurring subscription fees**.

---

## 👥 Who Can Use This Repository & Software?

### 1. Business Owners & Shopkeepers
* **Retail Stores & Supermarkets**: Fast POS counter billing, barcode scanning, 80mm thermal receipt printing, cash/UPI payment recording.
* **Wholesalers & Distributors**: Multi-item B2B invoices with customer GSTIN validation, delivery challans, credit/debit notes, and 0–90+ day aging reports.
* **Service Providers & Agencies**: Generate professional Quotations and Proforma Invoices, then convert them into GST Tax Invoices in 1-click once the client approves.
* **Businesses using Pre-Printed Letterhead**: Dedicated print mode that leaves a custom top margin gap so your bills print cleanly on your existing printed company stationery without overlapping.

### 2. Tax Practitioners & Accountants
* Instantly generate official **GSTR-1 JSON files** formatted to government offline tool specifications, ready for direct upload to `gst.gov.in`.
* Automated **GSTR-3B monthly tax computation matrix** showing outward supplies, RCM liabilities, and eligible Input Tax Credit (ITC).
* Comprehensive **Table 12 HSN/SAC summary** and **Table 13 documents issued register**.

### 3. Software Developers & Agencies
* **Use as a Ready Commercial Billing App**: Deploy for your retail/wholesale clients with complete confidence.
* **White-Label & Customize**: Add your own modules, customized print templates, or custom database backends.
* **Reference Implementation**: Learn how to build enterprise-grade, offline-first Windows applications using **.NET 10 (C#)**, **Angular 22**, **WebView2**, **Dapper**, and **QuestPDF**.

---

## 🎯 What Can You Do with BillMitra? (Key Features)

| What You Want to Do | How BillMitra Does It |
| :--- | :--- |
| **Create a GST Tax Invoice** | Pick customer & items; GST (CGST/SGST or IGST) is calculated automatically based on state codes and HSN tax slabs (0%, 5%, 12%, 18%, 28%). |
| **Convert Quotation to Invoice** | Send a Quotation or Proforma Invoice. When approved, click **Convert to Invoice**; a final Tax Invoice is generated instantly with reference audit links. |
| **E-Way Bill & E-Invoice Integration** | 1-click official government JSON export for E-Way Bill (EWB) and E-Invoice (IRN/QR code generation) with NIC sandbox & live APIs. |
| **Purchases & Operating Expenses** | Log vendor inward purchase bills, verify Input Tax Credit (ITC) eligibility, and record direct/indirect company expenses with cash outflow tracking. |
| **Multi-Godown Inventory & Batches** | Track stock across multiple warehouses/godowns, manage batch/lot numbers with expiry dates, execute inter-warehouse transfers and physical reconciliation. |
| **Executive Reports & Debt Aging** | View real-time gross/net profit metrics, 6-month financial performance trends, 0-90+ days receivable/payable aging matrix, and fast/slow product velocities. |
| **Automated Backups & Disaster Recovery** | Automated background database snapshots (SQLite VACUUM / WAL checkpoints) on startup, SHA256 checksums, retention policies, and safe point-in-time database restore. |
| **Print POS Thermal Receipts** | Click **Direct Print** to send an 80mm formatted thermal slip straight to your thermal printer (Epson, TVS, NGX, Posiflex) in milliseconds. |
| **Print on Pre-Printed Letterhead** | Enable "Pre-Printed Stationery Mode". BillMitra suppresses the top company details and leaves a configurable blank top margin (e.g., 50mm) for your pre-printed stationery. |
| **File Monthly GSTR-1** | Go to GST Returns, choose the month, and click **Download Portal JSON**. Upload the generated `GSTR1_{GSTIN}_{Period}.json` directly to the GST Common Portal. |
| **Track Customer Credit & Balances** | View real-time party ledgers. See who owes you money with 0–30, 31–60, 61–90, and 90+ day overdue aging analysis. |
| **Record Customer Payments** | Record payments (Cash, Bank Transfer, UPI, Cheque) and let BillMitra automatically settle outstanding invoices using FIFO allocation. |
| **Operate Without Internet** | Create thousands of bills without internet lag or downtime. Data is stored safely on your local disk (SQLite, SQL Server, or PostgreSQL). |

---

## 🚀 How to Use BillMitra (Step-by-Step Guide)

### Step 1: First-Time Setup
When you first launch BillMitra, the setup wizard helps you initialize your company:
1. Enter your **Business Legal Name** & **Trade Name**.
2. Enter your **GSTIN** (state code, state name, and PAN are automatically derived).
3. Enter your contact details, billing address, and default bank account details for invoice footers.
4. Set your administrator password.

### Step 2: Add Your Products & Customers
* **Items / Catalog**: Click **Items** $\rightarrow$ **Add Product**. Enter item name, SKU/Barcode, HSN/SAC code, unit of measure (PCS, KGS, BOX), GST tax rate (e.g., 18%), and selling price.
* **Customers / Parties**: Click **Customers** $\rightarrow$ **Add Party**. Select whether the party is **Registered (B2B)** or **Unregistered (Consumer)**. Enter their GSTIN, phone, and billing address.

### Step 3: Create an Invoice
1. Go to **Sales Invoices** $\rightarrow$ Click **New Invoice**.
2. Select Document Type: **Tax Invoice**, **Proforma Invoice**, or **Quotation**.
3. Select your customer from the dropdown (or create one on the fly).
4. Add line items: quantities, rates, and item discounts. The system automatically computes taxable values, CGST, SGST, IGST, and final invoice totals.
5. Click **Save as Draft** or **Finalize Document**. Finalizing assigns an audit-proof, gap-free statutory invoice number (e.g., `INV/26-27/0001`).

### Step 4: 1-Click Quotation / Proforma Conversion
1. In the invoice list, find any approved Quotation (`QT/...`) or Proforma Invoice (`PI/...`).
2. Click the **Convert to Tax Invoice** button.
3. BillMitra instantly creates a new official GST Tax Invoice containing all items and prices, locks the original quotation from duplicate conversions, and prints `"Order/Ref: PI/26-27/0001"` on the new bill.

### Step 5: Print & Preview
Click **Print / Preview** on any finalized invoice to open the interactive print dialog:
* **Choose Format**: Toggle between **Standard A4**, **Compact A5 Landscape**, or **80mm POS Thermal Slip**.
* **Choose Header Mode**: Toggle between **With Digital Letterhead** (renders full logo & company header) or **Pre-Printed Stationery** (leaves top margin space for your offset letterhead).
* Click **Direct Print** to send the job immediately to your Windows printer spooler.

### Step 6: Record Payments & View Ledgers
1. When a customer pays, go to **Payments & Receipts** $\rightarrow$ **Record Receipt**.
2. Enter the amount received and payment mode (Cash, Bank, UPI).
3. BillMitra automatically allocates the payment against the oldest unpaid invoices.
4. Open **Party Ledgers** to view the customer's running statement, total sales, payments received, and overdue aging breakdown (0–30, 31–60, 61–90, 90+ days).

### Step 7: Export GSTR-1 & Prepare GSTR-3B
1. Go to **GST Returns (GSTR-1/3B)** in the left menu.
2. Select the filing month and financial year.
3. Review your categorizations:
   * **Table 4A**: B2B Supplies
   * **Table 5A**: B2CL Large Invoices (> ₹2,50,000 inter-state)
   * **Table 7**: B2CS Counter Retail Sales
   * **Table 12**: HSN/SAC summary register
   * **Table 13**: Summary of all invoice numbers issued and cancelled
4. Click **Download Portal JSON** to get the official offline JSON file.
5. Log in to [gst.gov.in](https://www.gst.gov.in) $\rightarrow$ Returns Dashboard $\rightarrow$ Upload the JSON file.

### Step 8: Multi-Warehouse Inventory & Batch Tracking
1. Go to **Inventory & Godowns** $\rightarrow$ **Warehouse Locations** to manage branch and storage locations.
2. Under **Batch & Expiry Register**, register manufacturer lot numbers and expiry dates to avoid stock obsolescence.
3. Use **Physical Reconciliation** to adjust variance between physical stock count and system ledger in 1 click.
4. Execute **Inter-Warehouse Stock Transfers** with automatic audit logs.

### Step 9: Inward Purchases & Expenses
1. Go to **Inward Purchases** $\rightarrow$ **New Inward Purchase Bill** to register supplier bills and eligible Input Tax Credit (ITC).
2. Go to **Expenses & Cash** to log day-to-day office expenses (rent, electricity, transport, stationery) with GST categorization.

### Step 10: Automated Backups & Safe Restore
1. Go to **Backup & Restore** in the left menu.
2. Click **Create Instant Backup** anytime to take a complete point-in-time snapshot with SHA256 integrity verification.
3. Enable **Auto-Backup on Application Startup** so BillMitra automatically backs up your business every 24 hours.
4. Use **Restore Database** to recover previous snapshots safely with automatic pre-restore safety snapshots.

---

## 💻 System Requirements & Quick Start

### For End Users (Running the Application)
* **OS**: Windows 10 (64-bit) or Windows 11 (64-bit).
* **RAM**: 4 GB minimum (8 GB recommended).
* **Disk**: 500 MB free hard drive space.
* **Printers**: Any standard USB, LAN, or Wi-Fi printer (Laser, Inkjet, or 80mm/58mm Thermal POS Slip Printer).

👉 **[Download the Latest Windows Release](https://github.com/sankyn1/BillMitra/releases)** or visit the **[Official Website](https://sankyn1.github.io/BillMitra/)**.

---

### For Developers & Contributors (Building from Source)

#### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js 22+ LTS](https://nodejs.org/) & npm
* PowerShell 7 or Windows PowerShell 5.1

#### 1. Clone the Repository
```powershell
git clone https://github.com/sankyn1/BillMitra.git
cd BillMitra
```

#### 2. Run Automated Verification Tests
```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-tests.ps1
```

#### 3. Build Runnable Development Package (1 Command)
```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-development.ps1
```
This single command compiles the Angular 22 frontend, builds the .NET 10 backend, and packages the desktop shell into a runnable executable at:
```
artifacts/development/BillMitra.exe
```

#### 4. Live Hot-Reload Development (Optional)
* **Backend API**:
  ```powershell
  cd src/Backend/BillMitra.Api
  dotnet run
  ```
  Swagger API explorer available at `http://localhost:5000/swagger`.

* **Frontend UI**:
  ```powershell
  cd src/Frontend/billmitra-ui
  npm install
  npm start
  ```
  Angular web interface live at `http://localhost:4200`.

---

## 🗺️ Project Milestones & Current Status

* ✅ **MVP-0**: Engineering Foundation (.NET 10, Angular 22, Dapper, Architecture Tests)
* ✅ **MVP-1**: First-Run Setup Wizard & Company Profile Initialization
* ✅ **MVP-2**: Product Catalog, HSN/SAC Directory, Customer & Supplier Masters
* ✅ **MVP-3**: Central GST Engine, Gap-Free Sequential Numbering & QuestPDF Engine
* ✅ **MVP-4**: Receipts, Multi-Invoice FIFO Allocation, Party Ledgers & Aging Analysis
* ✅ **MVP-4.1**: Proforma Conversion, Print Preview, Direct Print & Letterhead Stationery Mode
* ✅ **MVP-5**: GST Returns (GSTR-1 Portal JSON Export, GSTR-3B Matrix, Table 12 HSN)
* 🚀 **MVP-6**: E-Way Bill & E-Invoice (IRN, Signed QR Code, NIC Sandbox) *(In Progress)*
* ⏳ **MVP-7**: Purchases & Expense Management (Inward ITC tracking)
* ⏳ **MVP-8**: Inventory Management (Multi-Warehouse, Batches, Expiry, FIFO Valuation)
* ⏳ **MVP-9**: Executive Business Dashboard & Financial Reports
* ⏳ **MVP-10**: Automated Local Backups & Cloud Sync
* ⏳ **MVP-11**: Inno Setup Windows Production Installer & Auto-Updater

Detailed architectural specifications and statutory reference rules are available in the [`docs/`](./docs) folder.

---

## 🤝 Contributing & Community

We welcome contributions from developers, accountants, and GST tax professionals!
* Found a bug or statutory edge case? [Open an Issue](https://github.com/sankyn1/BillMitra/issues).
* Want to contribute features? [Submit a Pull Request](https://github.com/sankyn1/BillMitra/pulls).

---

## ⭐ Star the Project

If you find BillMitra useful for your business, shop, or development work, please **star this repository on GitHub**! It helps more Indian small businesses discover a free, modern, offline-first alternative for their billing needs.

---

## 📄 License

Distributed under the **MIT License**. Free for commercial and personal use.  
Copyright © 2026 BillMitra Contributors.
