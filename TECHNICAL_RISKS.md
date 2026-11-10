# BillMitra – Technical Risks and Mitigation Matrix

**Product:** BillMitra – Windows GST Billing, Inventory, and Business Management Software  
**Version:** 1.0  
**Status:** Initial Assessment Completed  

---

## 1. Identified Technical Risks & Mitigation Strategies

| ID | Risk Category | Risk Description | Severity | Probability | Mitigation Strategy |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **TR-01** | **Data Integrity** | **Concurrent Invoice Numbering Collision**<br>Simultaneous finalization requests generating identical serial numbers, violating CGST Rule 46. | Critical | Low (single-PC), High (LAN/Multi-user) | Implement pessimistic locking using PostgreSQL `SELECT ... FOR UPDATE` on the specific `document_series` row within a strict database transaction. Complement with a unique constraint on `(financial_year_id, series_id, document_number)`. |
| **TR-02** | **Desktop Runtime** | **WebView2 Runtime Absent or Corrupted**<br>Target Windows machines without Edge WebView2 runtime failing to launch the desktop shell. | High | Medium | Include Evergreen WebView2 Bootstrapper in installer / check logic. Shell displays native error dialog with auto-download link if WebView2 is missing. |
| **TR-03** | **Database Ops** | **PostgreSQL Service Conflicts / Port Collisions**<br>Target machines with existing PostgreSQL on port 5432 conflicting with BillMitra. | High | Medium | The production installer configures a dedicated instance name (`BillMitraPG`) on a distinct port (defaulting to 5433 or dynamic selection) and dedicated data directory (`C:\ProgramData\BillMitra\Database`). |
| **TR-04** | **Financial Calculation** | **Rounding Discrepancies in GST Split**<br>Minor 1-paisa rounding errors between line-item tax sum and invoice-level tax calculations causing GST portal validation rejection. | High | High | Implement standard mathematical half-up rounding at the line item level, with explicit reconciliation and optional `RoundOff` adjustment field to match government portal schema validation rules exactly. |
| **TR-05** | **Integrations** | **Third-party GSP/IRP API Latency and Outages**<br>Network dropouts or slow government portal responses hanging the local billing workflow. | High | High | Make all statutory submissions asynchronous from the core invoice finalization. Finalize invoice locally first, store payload, and execute API calls via background worker with exponential retry and explicit submission status tracking. |
| **TR-06** | **Performance** | **Large Transaction Ledger Query Degradation**<br>Reports, inventory valuation, and ledgers slowing down after years of high-volume billing. | Medium | Medium | Maintain indexed foreign keys and composite indexes on `(document_date, customer_id, status)`. Implement pagination on all UI data tables. Maintain cached stock balance summary table reconciled against immutable ledger. |
| **TR-07** | **Backup & Storage** | **Silent Backup Failure / Corrupted Archive**<br>Backups written successfully to disk but containing incomplete data or failing restoration. | Critical | Medium | Implement automated SHA-256 manifest verification immediately after backup creation. Provide a periodic test-restore drill mechanism to a temporary isolated database. |
| **TR-08** | **Security** | **Local Database Credential Exposure**<br>Cleartext PostgreSQL passwords stored in client configuration files. | High | Medium | Use Windows Data Protection API (DPAPI) to encrypt local connection strings and sensitive API tokens at rest. Never expose database credentials to the Angular frontend. |
| **TR-09** | **Regulatory** | **Government GST Rule Changes (Thresholds, Rates, Schemas)**<br>CBIC updating e-invoicing turnover thresholds or GSTR-1 payload formats breaking software. | Medium | High | Decouple business logic from hardcoded values. Use versioned, effective-dated rule tables and configuration files for tax slabs, thresholds, and export schemas. |
| **TR-10** | **Process Lifecycle** | **Orphaned Kestrel Background Process**<br>Desktop shell closed abruptly, leaving the backend API process running and locking local ports. | Medium | Medium | The WPF Desktop Shell monitors the child API process using Windows Job Objects (which automatically terminates child processes when the parent shell process exits) or explicit mutual heartbeat monitoring. |

---

## 2. Review and Monitoring Schedule

This risk log will be re-evaluated at the conclusion of every MVP milestone. Any emerging risk must be logged with corresponding mitigation before proceeding to the subsequent phase.

