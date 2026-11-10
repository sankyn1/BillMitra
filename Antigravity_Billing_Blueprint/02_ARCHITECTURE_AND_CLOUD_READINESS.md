# 02 -- Architecture and Cloud Readiness

## Architecture style

Use a modular monolith with clear module boundaries. Do not use
microservices in the initial product.

## Logical architecture

Desktop shell (WebView2) -\> Angular 22 UI -\> ASP.NET Core .NET 10 API
(Kestrel) -\> Application services / domain rules -\> Dapper
repositories -\> PostgreSQL

Cross-cutting components: - Authentication and authorization. - Audit
logging. - Document/PDF generation. - File storage abstraction. - Backup
provider abstraction. - Email/WhatsApp provider abstractions. -
GST/GSP/IRP/E-Way Bill provider adapters. - Background job scheduler. -
Health checks and structured logging.

## Project structure

src/ Desktop/Billing.Desktop/ Frontend/billing-ui/ Backend/Billing.Api/
Backend/Billing.Application/ Backend/Billing.Domain/
Backend/Billing.Infrastructure/ Backend/Billing.Worker/
Shared/Billing.Contracts/ tests/ Billing.UnitTests/
Billing.IntegrationTests/ Billing.ApiTests/ Billing.ArchitectureTests/
database/ migrations/ seed/ deployment/ windows-installer/ scripts/
docs/

## Layer responsibilities

-   Domain: entities, value objects, invariants, financial and inventory
    rules.
-   Application: use cases, commands, queries, DTO orchestration,
    interfaces.
-   Infrastructure: Dapper, PostgreSQL, file/object storage, external
    providers, PDF, email.
-   API: HTTP endpoints, authentication, validation pipeline, response
    mapping.
-   Worker: scheduled reminders, backup orchestration, integration
    retries.
-   Frontend: presentation, client-side validation and API interaction
    only.
-   Desktop: application lifecycle, local service health, WebView2
    hosting and OS integration.

## Cloud portability rules

1.  Use configuration providers, not hard-coded local paths.
2.  Define `IFileStorage`, `IBackupStorageProvider`, `IEmailProvider`,
    `IWhatsAppProvider`, `IGstReturnProvider`, `IEInvoiceProvider` and
    `IEWayBillProvider`.
3.  Store document metadata in PostgreSQL; store file bytes through an
    abstract storage service.
4.  Local storage implementation uses configured directories. Cloud
    implementation can use S3-compatible object storage.
5.  Never expose local filesystem paths to frontend as the storage
    contract. Return document IDs or short-lived access URLs.
6.  Keep database access behind repositories and application services.
7.  Use standard HTTP APIs and OpenAPI contracts.
8.  Use UTC timestamps; render in configured local timezone.
9.  Use configuration and secret providers; never embed credentials in
    source or frontend.
10. Use background jobs that can run in a Windows worker initially and a
    cloud worker later.
11. Avoid Windows-only APIs in domain/application layers.
12. Use provider adapters so GST/GSP/IRP vendors can be changed without
    changing invoice logic.

## Local deployment

-   ASP.NET Core hosted as a Windows Service using Kestrel.
-   API listens on loopback only in single-PC mode.
-   Angular static assets served by the API or desktop shell.
-   PostgreSQL installed/configured by installer as a dedicated
    instance/service.
-   Desktop shell starts after service health check.
-   No IIS required.
-   Do not expose PostgreSQL to LAN or internet in single-PC mode.

## Future cloud deployment

The same API and application services can be deployed behind a reverse
proxy/load balancer. Replace local storage providers with object
storage, local secrets with managed secrets, and local database with
managed PostgreSQL. Add tenant/business isolation before offering a
multi-business SaaS edition. Do not assume single-business schema is
already safe for multi-tenant SaaS.

## API conventions

-   Version APIs: `/api/v1/...`.
-   Use DTOs, not database entities, at API boundaries.
-   Consistent problem/error response.
-   Pagination and filtering conventions.
-   Idempotency keys for operations that may be retried.
-   Optimistic concurrency for editable drafts/settings.
-   OpenAPI generated and version-controlled.
-   Do not break released endpoints; introduce versioned changes or
    backward-compatible additions.

## Security

-   Passwords hashed with a modern password hasher.
-   Secrets protected using OS-protected storage or encrypted
    configuration.
-   Local API requires authenticated requests and appropriate
    CSRF/origin protections for the desktop context.
-   Bind loopback only in single-PC mode.
-   Do not store database superuser credentials in application config.
-   Use a restricted database role for runtime.
-   Audit sensitive actions.
