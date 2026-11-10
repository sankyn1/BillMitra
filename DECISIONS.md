# Architecture Decision Log

## ADR-001 -- Modular monolith

Status: Accepted  
Decision: Use a modular monolith initially.  
Reason: Easier deployment, debugging and maintenance for a single-business Windows product. Module boundaries preserve future extraction options.

## ADR-002 -- Local-first API architecture

Status: Accepted  
Decision: Angular communicates only with ASP.NET Core API; API accesses PostgreSQL.  
Reason: Same API/business logic can be hosted locally or in cloud.

## ADR-003 -- No IIS

Status: Accepted  
Decision: Host Kestrel as a Windows Service.  
Reason: Reduce customer installation and administration burden.

## ADR-004 -- Dapper runtime, EF migrations

Status: Accepted  
Decision: Dapper for runtime data access; EF Core migrations only.  
Reason: Matches product-owner preference while retaining migration tooling.

## ADR-005 -- Financial document immutability

Status: Accepted  
Decision: Finalized financial documents are immutable; corrections use cancellation/adjustment workflows.  
Reason: Auditability and statutory integrity.

## ADR-006 -- Invoice numbering

Status: Accepted  
Decision: Server-side atomic numbering, financial-year series, statutory validation and permanent numbers after issue.  
Reason: Avoid duplicates and maintain GST compliance.

## ADR-007 -- Provider adapters

Status: Accepted  
Decision: GST, GSP, IRP, E-Way Bill, storage and notification providers are abstracted behind interfaces.  
Reason: Avoid vendor lock-in and ease cloud migration.

## ADR-008 -- Windows Installer Technology (Inno Setup)

Status: Accepted  
Decision: Select Inno Setup for the customer-facing Windows installer.  
Reason: Inno Setup provides reliable, scriptable silent installation of bundled prerequisites (PostgreSQL dedicated instance, WebView2 Evergreen bootstrapper), native service registration, custom port detection, and upgrade safety without the complexity and brittle custom actions of WiX/MSI.

## ADR-009 -- Local Development Database Defaults

Status: Accepted  
Decision: Use standard local development connection string `Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;` with override support via environment variables and `appsettings.Development.json`.  
Reason: Enables frictionless developer onboarding while maintaining strict isolation from production configurations.

## ADR-010 -- Desktop Shell Process Supervision in Development

Status: Accepted  
Decision: The WPF Desktop Shell in development testing builds supervises the Kestrel API as a managed child process using Windows Job Objects when the Windows Service is not installed.  
Reason: Allows testing the compiled desktop executable and Angular UI without requiring elevated administrative privileges to install Windows Services on developer machines.

## ADR-011 -- Thermal Receipt Template in MVP-3

Status: Accepted  
Decision: Provide an 80mm thermal receipt PDF and print layout in MVP-3 alongside standard A4/A5 tax invoices.  
Reason: Accommodates retail and counter billing users who require immediate thermal receipt printing without waiting for advanced POS features in MVP-12.

