# 05 -- GST, E-Way Bill and E-Invoice Integrations

## General rule

Core business documents and calculations must work independently of
external government/provider APIs. Integrations are adapters around the
core domain.

Do not hard-code one GSP, IRP or E-Way Bill provider into invoice
services.

## GST return workflow

1.  Validate source sales/purchase documents.
2.  Classify supplies and tax treatment.
3.  Prepare GSTR-1 data.
4.  Validate required fields and totals.
5.  Generate supported export payload/file.
6.  Prepare GSTR-3B summary.
7.  Import/reconcile GSTR-2B where supported.
8.  Review tax liability and ITC differences.
9.  Submit through authorized provider if configured.
10. Store provider request reference, response, ARN/acknowledgement and
    filing status.
11. Reconcile filed values to books.

## Submission status model

-   NotPrepared
-   Prepared
-   ValidationFailed
-   ReadyForSubmission
-   SubmittedToProvider
-   Pending
-   Accepted
-   Rejected
-   Filed/Completed (only where authoritative confirmation exists)
-   Cancelled/Amended where legally supported

Do not equate a successful HTTP response with filed status. Parse and
validate provider acknowledgement.

## GST integration abstraction

`IGstReturnProvider` - Validate credentials/registration. -
Prepare/validate payload. - Submit return. - Get submission status. -
Fetch acknowledgement. - Retrieve supported portal data where
authorized.

Store provider name/version, taxpayer consent/reference, period, request
ID, response metadata, timestamps and status. Redact tokens and
sensitive values from logs.

## E-Way Bill

Support: - Manual data entry and tracking. - Generation from eligible
invoice/challan. - Part A and Part B details. - Transporter ID and
vehicle data. - Mode and distance. - API generation. - Status lookup. -
Cancellation where permitted. - Vehicle update and extension where
permitted. - Consolidated E-Way Bill if provider and regulations support
it. - Validity/expiry notifications.

Never hard-code thresholds, exemptions, validity formulas or
cancellation windows in UI code. Use versioned, effective-dated
rules/configuration and validate against current official rules before
production.

Official references: - CBIC E-Way Bill rules:
https://cbic-gst.gov.in/ewaybill-rules.html - E-Way Bill official FAQ:
https://docs.ewaybillgst.gov.in/html/faq1.html

## E-Invoice

Support: - Eligibility check using effective-dated rules. - Generate IRN
through authorized IRP integration. - Store IRN, acknowledgement
number/date. - Store signed invoice data and signed QR payload
securely. - Validate signed response. - IRN status lookup. -
Cancellation where permitted. - E-Invoice PDF rendering. -
Reconciliation of local invoice and IRP response.

Never fabricate an IRN or signed QR. If API access is unavailable,
clearly mark the document as locally prepared/not submitted.

## Provider resilience

-   Timeouts and retry policy for transient failures.
-   Idempotency to prevent duplicate generation/submission.
-   Circuit breaker where appropriate.
-   Store request/response correlation IDs.
-   Do not retry non-idempotent operations blindly.
-   Queue retryable operations with visible status.
-   Manual reconciliation path for uncertain outcomes.
-   Provider sandbox and production environments must be separated.

## GST rule maintenance

GST rates, thresholds, return schemas and e-document rules change. Store
effective dates and version identifiers. Keep a reviewed rule update
process. Require domain review before production updates.
