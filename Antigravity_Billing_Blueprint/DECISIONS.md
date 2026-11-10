# Architecture Decision Log

## ADR-001 -- Modular monolith

Status: Accepted\
Decision: Use a modular monolith initially.\
Reason: Easier deployment, debugging and maintenance for a
single-business Windows product. Module boundaries preserve future
extraction options.

## ADR-002 -- Local-first API architecture

Status: Accepted\
Decision: Angular communicates only with ASP.NET Core API; API accesses
PostgreSQL.\
Reason: Same API/business logic can be hosted locally or in cloud.

## ADR-003 -- No IIS

Status: Accepted\
Decision: Host Kestrel as a Windows Service.\
Reason: Reduce customer installation and administration burden.

## ADR-004 -- Dapper runtime, EF migrations

Status: Accepted\
Decision: Dapper for runtime data access; EF Core migrations only.\
Reason: Matches product-owner preference while retaining migration
tooling.

## ADR-005 -- Financial document immutability

Status: Accepted\
Decision: Finalized financial documents are immutable; corrections use
cancellation/adjustment workflows.\
Reason: Auditability and statutory integrity.

## ADR-006 -- Invoice numbering

Status: Accepted\
Decision: Server-side atomic numbering, financial-year series, statutory
validation and permanent numbers after issue.\
Reason: Avoid duplicates and maintain GST compliance.

## ADR-007 -- Provider adapters

Status: Accepted\
Decision: GST, GSP, IRP, E-Way Bill, storage and notification providers
are abstracted behind interfaces.\
Reason: Avoid vendor lock-in and ease cloud migration.
