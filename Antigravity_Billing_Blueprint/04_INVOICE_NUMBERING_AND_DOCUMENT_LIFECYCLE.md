# 04 -- Invoice Numbering and Document Lifecycle

## Regulatory baseline

For GST tax invoices, CGST Rules Rule 46 requires a consecutive serial
number, not exceeding 16 characters, using permitted characters
(letters, numerals, hyphen and slash) and unique for a financial year.
Multiple series are permitted. Confirm current rules and notifications
with a qualified GST professional before release.

Official reference: CBIC GST invoice rules --
https://cbic-gst.gov.in/gst-invoice-rules.html\
Rule 46:
https://taxinformation.cbic.gov.in/content-page/explore-rules/1000136/1000001

## Recommended default invoice series

Keep the GST invoice number within 16 characters.

Recommended default: `INV/26-27/0001`

Character count: 14. Examples: - `INV/26-27/0001` - `INV/26-27/0002` -
`INV/26-27/0003`

Do not default to long prefixes such as `TAX-INVOICE/2026-27/000001`,
which can exceed the 16-character limit.

## User-configurable series

Allow the owner to configure: - Document type. - Prefix. - Financial
year token. - Sequence start. - Padding length. - Optional
branch/location series. - Reset policy. - Effective date. - Whether a
series is active.

Validate the final generated serial number against applicable statutory
character and length constraints for the relevant document type. For GST
tax invoices, enforce Rule 46 constraints. Apply corresponding current
rules to bill of supply and credit/debit notes.

## Numbering examples

GST invoice: `INV/26-27/0001`\
Bill of supply: `BOS/26-27/0001`\
Credit note: `CN/26-27/0001`\
Debit note: `DN/26-27/0001`\
Quotation: `QT/26-27/0001`\
Proforma: `PI/26-27/0001`\
Delivery challan: `DC/26-27/0001`\
Receipt voucher: `RV/26-27/0001`

These are suggested defaults, not mandatory legal formats. The tax
invoice series must remain compliant with its statutory constraints.

## Financial year

For India, default financial year is 1 April to 31 March, but store
explicit start/end dates rather than hard-coding date arithmetic
everywhere.

At financial-year rollover: - Create or activate the new financial
year. - Create new numbering series for the new FY. - Preserve all
prior-year numbers and documents. - Do not modify old finalized document
numbers. - Allow authorized user to configure series before first
document is issued.

## Atomic number allocation

Number allocation must be performed server-side in a database
transaction using a locked sequence row or equivalent concurrency-safe
mechanism.

Requirements: - Two simultaneous users/processes cannot receive the same
number. - Do not allocate numbers in Angular. - Do not calculate next
number using `MAX(number)+1`. - Store sequence state separately from
documents. - Enforce a unique database constraint on the applicable
series and final document number. - Record allocation time, user and
document. - Handle failed transactions without leaving an
issued/finalized document in an inconsistent state.

## Drafts versus finalized documents

Recommended policy: - Drafts may use temporary draft IDs and must not
consume statutory invoice numbers unless the owner explicitly chooses a
legally reviewed policy. - Allocate the final invoice number when the
document is finalized/issued. - Once finalized, the number is permanent
and never reused, even if cancelled. - Preserve cancellation record and
reason. - Never delete a finalized document. - If a finalization
transaction fails before commit, no finalized document should exist and
the sequence update must roll back atomically. - For a finalized invoice
that is later cancelled, retain the number and show cancelled status.

## Document lifecycle

Draft -\> Validated -\> Finalized/Issued -\> Partially Paid/Paid -\>
Closed\
Alternative transition: Finalized/Issued -\> Cancelled (only under
permitted rules and with audit record).

Do not allow arbitrary state changes. Use explicit transition commands
and permission checks.

## Document snapshot

At finalization snapshot: - Supplier/company legal name, address and
GSTIN. - Recipient name, address and GSTIN. - Place of supply and state
code. - Item description, HSN/SAC, quantity, unit. - Unit price,
discount, taxable value. - Tax rates and tax amounts. - Additional
charges and totals. - Terms and payment details. - Document date, number
and references.

Historical PDFs must be reproducible from the snapshot, not current
customer/product master values.

## Credit/debit notes

-   Preserve original invoice reference and date where required.
-   Store reason/nature and taxable/tax adjustments.
-   Prevent adjustments exceeding applicable original balances unless an
    authorized rule allows it.
-   Keep GST reporting linkage.
-   Keep inventory effect explicit and separate from financial effect.

## Idempotency

Finalization endpoint must accept an idempotency key or equivalent
operation token so retries do not create a second invoice.

## Audit

Record creator, finalizer, timestamps, changes to drafts, cancellation
reason, original document reference, external IRN/acknowledgement
references and API events.
