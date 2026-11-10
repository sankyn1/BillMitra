# 07 -- UI/UX Standards

## Product design goal

A premium, modern desktop business application that a non-technical
Indian business owner can learn quickly.

## Visual language

-   Clean, modern, restrained.
-   Light theme by default; optional dark theme.
-   Neutral surfaces with one primary brand color and semantic status
    colors.
-   Consistent spacing and typography.
-   Clear contrast and visible focus states.
-   Avoid excessive gradients, shadows, rounded cards and decorative
    graphics.
-   Avoid dense dashboard overload.
-   Use icons consistently and always pair ambiguous icons with labels.

## Application shell

-   Collapsible left sidebar.
-   Top bar with current company, global search, notifications and user
    menu.
-   Breadcrumbs on deep pages.
-   Main content area with consistent page title/action area.
-   Persistent app version and connection/backup status in suitable
    settings/diagnostics area.

## Navigation

Dashboard Sales & Billing - Invoices - Quotations - Proforma -
Credit/Debit Notes - Delivery Challans E-Way Bill E-Invoice GST Returns
Purchases Products & Inventory Customers Suppliers Payments &
Outstanding Expenses Reports Backup & Restore Users & Permissions
Settings

Use progressive disclosure. Do not show every low-level feature at once.

## Dashboard

Prioritize: - Today's sales. - Monthly sales. -
Receivables/outstanding. - Overdue invoices. - Recent
invoices/payments. - Low-stock and expiry alerts. - GST filing and E-Way
Bill alerts. - Backup health.

Every KPI must link to the relevant filtered list. Avoid charts without
a clear business question.

## Billing screen

-   Customer selector with search and quick-add.
-   Billing/shipping address selection.
-   Document type and date.
-   Item grid with keyboard-first entry.
-   Searchable product picker.
-   Quantity, unit, rate, discount, HSN/SAC, tax rate.
-   Clear tax breakup.
-   Additional charges.
-   Grand total and amount in words.
-   Notes and payment terms.
-   Save Draft, Preview, Finalize, Print and Share actions.
-   Sticky totals/action area where helpful.
-   Confirm finalization and explain that issued document number is
    permanent.

## Forms

-   Label every field.
-   Mark required fields.
-   Use contextual examples.
-   Validate on blur and submit without noisy errors on initial load.
-   Preserve user input on recoverable failures.
-   Show inline field-level errors.
-   Use sensible defaults but do not guess legal/tax data.

## Tables

-   Search, sort, filter, pagination.
-   Column visibility where useful.
-   Sticky header for long lists.
-   Row actions in a consistent location.
-   Status labels with text, not color alone.
-   Bulk actions only where safe.
-   Export respects current filters and permissions.

## Accessibility and usability

-   Full keyboard navigation.
-   Predictable tab order.
-   Keyboard shortcuts documented.
-   Adequate font sizes.
-   High contrast.
-   Tooltips for technical terms.
-   Friendly language and actionable errors.
-   Confirm destructive or irreversible operations.
-   Empty state explains what the user can do next.

## Responsive behavior

Desktop-first but usable on smaller screens. Do not assume one fixed
screen resolution. Avoid horizontal overflow for core tasks; wide grids
may have controlled horizontal scrolling.

## UX acceptance

Create clickable screen prototypes for dashboard, invoice editor,
customer details, outstanding, GST return status, E-Way Bill status and
backup/restore before building all screens. Get product-owner approval
on the core design system.
