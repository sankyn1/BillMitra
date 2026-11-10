# BillMitra – Open Questions & Decisions Requiring Approval

**Product:** BillMitra – Windows GST Billing, Inventory, and Business Management Software  
**Version:** 1.0  
**Status:** Pending Product Owner / Technical Lead Alignment  

---

## 1. Architectural & Technical Decisions Requiring Approval

### Question 1: Windows Installer Technology (Inno Setup vs. WiX Toolset)
- **Context:** Document 00 and Document 09 specify: *"Inno Setup or WiX for Windows installer; choose one and document why."*
- **Recommendation:** Choose **Inno Setup**. It provides native Pascal scripting to silently execute PostgreSQL and WebView2 runtime installers, inspect existing system services, configure custom ports, and manage registry/shortcuts with minimal overhead compared to complex WiX MSI custom actions.
- **Approval Needed:** Confirm Inno Setup as the chosen installer engine for recording as ADR-008.

### Question 2: Local Developer Database Connection Defaults
- **Context:** For the Day-1 runnable development package (`artifacts/development/`), the application must connect to a local PostgreSQL instance.
- **Recommendation:** Default development connection string to `Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;` with environment override support via `appsettings.Development.json` and CLI arguments.
- **Approval Needed:** Confirm this local development database convention.

### Question 3: Development Shell Process Supervision
- **Context:** Production deployment uses a registered Windows Service (`Billing.Service`), but during active MVP development, requiring an installed Windows Service slows down developer testing and demands administrative elevation.
- **Recommendation:** In development mode, `BillMitra.Desktop` checks if the Windows Service is active; if not, it automatically starts `BillMitra.Api.exe` as a child process using a Windows Job Object (ensuring clean auto-termination when the UI closes).
- **Approval Needed:** Confirm this development execution model.

---

## 2. Business & Regulatory Domain Clarifications

### Question 4: Composition Scheme Invoicing Policy
- **Context:** Indian GST law prohibits Composition Scheme dealers from issuing Tax Invoices and collecting tax.
- **Recommendation:** If a company's registration type is set to **Composition**, the UI will disable the "Tax Invoice" document type, enforce "Bill of Supply" generation with zero tax rates, and mandate the statutory disclaimer on all printed PDFs.
- **Approval Needed:** Confirm this domain rule enforcement.

### Question 5: Thermal / POS Receipt Printing Timing
- **Context:** Document 03 lists POS and thermal printing under MVP-12 (Advanced/Future), but retail users frequently request 80mm/58mm thermal receipts in MVP-3.
- **Recommendation:** Keep full POS workflow in MVP-12, but provide a 3-inch (80mm) thermal PDF/print template in MVP-3 alongside standard A4/A5 invoice templates.
- **Approval Needed:** Confirm whether 80mm thermal print format should be included in MVP-3 or deferred to MVP-12.

### Question 6: Statutory Integration Sandbox Providers (MVP-5 & MVP-6)
- **Context:** External API integration requires GSP/IRP sandbox credentials for E-Way Bill and E-Invoice testing.
- **Recommendation:** Build clean mock/sandbox adapters in MVP-0 to MVP-5, and connect to a specific designated provider (e.g., NIC Direct Sandbox, ClearTax, Masters India, or Cygnet) once provider credentials are provided.
- **Approval Needed:** Confirm any designated GSP/IRP partner preference.

---

## 3. Decision Log Updates
Once the above questions are answered, approved items will be formally recorded into `DECISIONS.md`.

