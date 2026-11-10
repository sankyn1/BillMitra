using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PaymentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string paymentSql = @"
SELECT 
    id as Id,
    payment_number as PaymentNumber,
    party_id as PartyId,
    party_name as PartyName,
    payment_type as PaymentType,
    payment_mode as PaymentMode,
    payment_date as PaymentDate,
    amount as Amount,
    allocated_amount as AllocatedAmount,
    reference_number as ReferenceNumber,
    bank_name as BankName,
    cheque_date as ChequeDate,
    notes as Notes,
    status as Status,
    created_at_utc as CreatedAtUtc,
    created_by as CreatedBy
FROM payments
WHERE id = @Id;";

        var payment = await conn.QuerySingleOrDefaultAsync<Payment>(paymentSql, new { Id = id });
        if (payment == null) return null;

        payment.Allocations = (await GetAllocationsByPaymentIdAsync(payment.Id, cancellationToken)).ToList();
        return payment;
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(
        int? paymentType = null,
        int? status = null,
        Guid? partyId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = @"
SELECT 
    id as Id,
    payment_number as PaymentNumber,
    party_id as PartyId,
    party_name as PartyName,
    payment_type as PaymentType,
    payment_mode as PaymentMode,
    payment_date as PaymentDate,
    amount as Amount,
    allocated_amount as AllocatedAmount,
    reference_number as ReferenceNumber,
    bank_name as BankName,
    cheque_date as ChequeDate,
    notes as Notes,
    status as Status,
    created_at_utc as CreatedAtUtc,
    created_by as CreatedBy
FROM payments
WHERE 1=1";

        var parameters = new DynamicParameters();

        if (paymentType.HasValue)
        {
            sql += " AND payment_type = @Type";
            parameters.Add("Type", paymentType.Value);
        }

        if (status.HasValue)
        {
            sql += " AND status = @Status";
            parameters.Add("Status", status.Value);
        }

        if (partyId.HasValue)
        {
            sql += " AND party_id = @PartyId";
            parameters.Add("PartyId", partyId.Value);
        }

        if (fromDate.HasValue)
        {
            sql += " AND payment_date >= @FromDate";
            parameters.Add("FromDate", fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            sql += " AND payment_date <= @ToDate";
            parameters.Add("ToDate", toDate.Value.Date);
        }

        sql += " ORDER BY payment_date DESC, created_at_utc DESC;";

        var list = (await conn.QueryAsync<Payment>(sql, parameters)).ToList();
        return list;
    }

    public async Task<Payment> InsertPaymentWithAllocationsAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        const string paymentSql = @"
INSERT INTO payments (
    id, payment_number, party_id, party_name, payment_type,
    payment_mode, payment_date, amount, allocated_amount,
    reference_number, bank_name, cheque_date, notes,
    status, created_at_utc, created_by
) VALUES (
    @Id, @PaymentNumber, @PartyId, @PartyName, @PaymentType,
    @PaymentMode, @PaymentDate, @Amount, @AllocatedAmount,
    @ReferenceNumber, @BankName, @ChequeDate, @Notes,
    @Status, @CreatedAtUtc, @CreatedBy
);";

        await conn.ExecuteAsync(paymentSql, new
        {
            payment.Id,
            payment.PaymentNumber,
            payment.PartyId,
            payment.PartyName,
            PaymentType = (int)payment.PaymentType,
            PaymentMode = (int)payment.PaymentMode,
            payment.PaymentDate,
            payment.Amount,
            payment.AllocatedAmount,
            payment.ReferenceNumber,
            payment.BankName,
            payment.ChequeDate,
            payment.Notes,
            Status = (int)payment.Status,
            payment.CreatedAtUtc,
            payment.CreatedBy
        }, tx);

        const string allocSql = @"
INSERT INTO payment_allocations (
    id, payment_id, sales_document_id, allocated_amount,
    allocation_date, notes, created_at_utc
) VALUES (
    @Id, @PaymentId, @SalesDocumentId, @AllocatedAmount,
    @AllocationDate, @Notes, @CreatedAtUtc
);";

        const string updateDocSql = @"
UPDATE sales_documents
SET 
    paid_amount = paid_amount + @AllocatedAmount,
    document_status = CASE 
        WHEN (paid_amount + @AllocatedAmount) >= grand_total THEN 4 
        ELSE 3 
    END
WHERE id = @SalesDocId;";

        foreach (var alloc in payment.Allocations)
        {
            await conn.ExecuteAsync(allocSql, new
            {
                alloc.Id,
                alloc.PaymentId,
                alloc.SalesDocumentId,
                alloc.AllocatedAmount,
                alloc.AllocationDate,
                alloc.Notes,
                alloc.CreatedAtUtc
            }, tx);

            await conn.ExecuteAsync(updateDocSql, new
            {
                alloc.AllocatedAmount,
                SalesDocId = alloc.SalesDocumentId
            }, tx);
        }

        tx.Commit();
        return payment;
    }

    public async Task<bool> ReversePaymentAsync(Guid paymentId, string reason, string? reversedBy, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        const string checkSql = "SELECT status FROM payments WHERE id = @Id;";
        var currentStatus = await conn.QuerySingleOrDefaultAsync<int?>(checkSql, new { Id = paymentId }, tx);

        if (!currentStatus.HasValue || currentStatus.Value == (int)PaymentStatus.Reversed)
        {
            return false;
        }

        // 1. Record reversal audit record
        const string reversalSql = @"
INSERT INTO payment_reversals (
    id, payment_id, reversal_date, reason, reversed_by, created_at_utc
) VALUES (
    @Id, @PaymentId, @ReversalDate, @Reason, @ReversedBy, @CreatedAtUtc
);";

        await conn.ExecuteAsync(reversalSql, new
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            ReversalDate = DateTime.UtcNow,
            Reason = reason,
            ReversedBy = reversedBy,
            CreatedAtUtc = DateTime.UtcNow
        }, tx);

        // 2. Fetch existing allocations
        const string getAllocsSql = "SELECT sales_document_id as SalesDocumentId, allocated_amount as AllocatedAmount FROM payment_allocations WHERE payment_id = @PaymentId;";
        var allocations = (await conn.QueryAsync<(Guid SalesDocId, decimal AllocatedAmount)>(getAllocsSql, new { PaymentId = paymentId }, tx)).ToList();

        // 3. Roll back paid_amount and restore document status
        const string rollbackDocSql = @"
UPDATE sales_documents
SET 
    paid_amount = CASE 
        WHEN (paid_amount - @AllocatedAmount) < 0 THEN 0 
        ELSE (paid_amount - @AllocatedAmount) 
    END,
    document_status = CASE 
        WHEN (paid_amount - @AllocatedAmount) <= 0 THEN 2 
        ELSE 3 
    END
WHERE id = @SalesDocId;";

        foreach (var alloc in allocations)
        {
            await conn.ExecuteAsync(rollbackDocSql, new
            {
                alloc.AllocatedAmount,
                alloc.SalesDocId
            }, tx);
        }

        // 4. Mark payment as reversed
        const string updatePaymentSql = "UPDATE payments SET status = @ReversedStatus WHERE id = @Id;";
        await conn.ExecuteAsync(updatePaymentSql, new { ReversedStatus = (int)PaymentStatus.Reversed, Id = paymentId }, tx);

        tx.Commit();
        return true;
    }

    public async Task<IReadOnlyList<PaymentAllocation>> GetAllocationsByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
SELECT 
    pa.id as Id,
    pa.payment_id as PaymentId,
    pa.sales_document_id as SalesDocumentId,
    pa.allocated_amount as AllocatedAmount,
    pa.allocation_date as AllocationDate,
    pa.notes as Notes,
    pa.created_at_utc as CreatedAtUtc,
    sd.document_number as DocumentNumber,
    sd.document_date as DocumentDate,
    sd.grand_total as InvoiceGrandTotal,
    sd.paid_amount as PreviouslyPaid
FROM payment_allocations pa
INNER JOIN sales_documents sd ON sd.id = pa.sales_document_id
WHERE pa.payment_id = @PaymentId
ORDER BY pa.allocation_date ASC;";

        var list = (await conn.QueryAsync<PaymentAllocation>(sql, new { PaymentId = paymentId })).ToList();
        return list;
    }

    public async Task<IReadOnlyList<PaymentAllocation>> GetAllocationsBySalesDocumentIdAsync(Guid salesDocumentId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
SELECT 
    pa.id as Id,
    pa.payment_id as PaymentId,
    pa.sales_document_id as SalesDocumentId,
    pa.allocated_amount as AllocatedAmount,
    pa.allocation_date as AllocationDate,
    pa.notes as Notes,
    pa.created_at_utc as CreatedAtUtc,
    sd.document_number as DocumentNumber,
    sd.document_date as DocumentDate,
    sd.grand_total as InvoiceGrandTotal,
    sd.paid_amount as PreviouslyPaid
FROM payment_allocations pa
INNER JOIN sales_documents sd ON sd.id = pa.sales_document_id
WHERE pa.sales_document_id = @SalesDocId
ORDER BY pa.allocation_date ASC;";

        var list = (await conn.QueryAsync<PaymentAllocation>(sql, new { SalesDocId = salesDocumentId })).ToList();
        return list;
    }

    public async Task<IReadOnlyList<SalesDocument>> GetOpenInvoicesForPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_status as DocumentStatus,
    document_number as DocumentNumber,
    draft_number as DraftNumber,
    document_date as DocumentDate,
    due_date as DueDate,
    party_id as PartyId,
    customer_name as CustomerName,
    grand_total as GrandTotal,
    paid_amount as PaidAmount
FROM sales_documents
WHERE party_id = @PartyId 
  AND document_status IN (2, 3) 
  AND (grand_total - paid_amount) > 0.001
ORDER BY document_date ASC, created_at_utc ASC;";

        var list = (await conn.QueryAsync<SalesDocument>(sql, new { PartyId = partyId })).ToList();
        return list;
    }

    public async Task<IReadOnlyList<LedgerEntry>> GetPartyLedgerAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Fetch finalized sales invoices for party
        const string docSql = @"
SELECT 
    document_date as Date,
    'Invoice' as VoucherType,
    COALESCE(document_number, draft_number) as VoucherNumber,
    customer_name as Reference,
    grand_total as DebitAmount,
    0.00 as CreditAmount,
    notes as Notes,
    created_at_utc as CreatedAtUtc
FROM sales_documents
WHERE party_id = @PartyId 
  AND document_status NOT IN (1, 5);";

        var invoices = (await conn.QueryAsync<dynamic>(docSql, new { PartyId = partyId })).ToList();

        // Fetch payments for party
        const string paymentSql = @"
SELECT 
    p.payment_date as Date,
    CASE WHEN p.payment_type = 1 THEN 'Receipt' ELSE 'Payment' END as VoucherType,
    p.payment_number as VoucherNumber,
    p.reference_number as Reference,
    CASE WHEN p.payment_type = 2 THEN p.amount ELSE 0.00 END as DebitAmount,
    CASE WHEN p.payment_type = 1 THEN p.amount ELSE 0.00 END as CreditAmount,
    p.notes as Notes,
    p.status as Status,
    p.created_at_utc as CreatedAtUtc,
    pr.reversal_date as ReversalDate,
    pr.reason as ReversalReason
FROM payments p
LEFT JOIN payment_reversals pr ON pr.payment_id = p.id
WHERE p.party_id = @PartyId;";

        var payments = (await conn.QueryAsync<dynamic>(paymentSql, new { PartyId = partyId })).ToList();

        var entries = new List<(DateTime Date, DateTime CreatedAt, string VoucherType, string VoucherNumber, string? Reference, decimal Debit, decimal Credit, string? Notes)>();

        foreach (var inv in invoices)
        {
            entries.Add((
                (DateTime)inv.Date,
                (DateTime)inv.CreatedAtUtc,
                (string)inv.VoucherType,
                (string)inv.VoucherNumber,
                (string?)inv.Reference,
                (decimal)inv.DebitAmount,
                (decimal)inv.CreditAmount,
                (string?)inv.Notes
            ));
        }

        foreach (var p in payments)
        {
            int status = (int)p.Status;
            entries.Add((
                (DateTime)p.Date,
                (DateTime)p.CreatedAtUtc,
                (string)p.VoucherType,
                (string)p.VoucherNumber,
                (string?)p.Reference,
                (decimal)p.DebitAmount,
                (decimal)p.CreditAmount,
                (string?)p.Notes
            ));

            if (status == (int)PaymentStatus.Reversed && p.ReversalDate != null)
            {
                // Reversal reverses the credit/debit
                entries.Add((
                    (DateTime)p.ReversalDate,
                    (DateTime)p.ReversalDate,
                    "Reversal",
                    "REV-" + (string)p.VoucherNumber,
                    "Cancelled Payment",
                    (decimal)p.CreditAmount, // If receipt (was credit), reversal is debit
                    (decimal)p.DebitAmount,  // If outward payment (was debit), reversal is credit
                    "Reversal Reason: " + (string?)p.ReversalReason
                ));
            }
        }

        // Sort chronologically
        var sorted = entries.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList();

        var ledger = new List<LedgerEntry>();
        decimal runningBalance = 0;

        foreach (var item in sorted)
        {
            runningBalance += (item.Debit - item.Credit);

            // Apply optional date filter after calculating cumulative effect
            if (fromDate.HasValue && item.Date < fromDate.Value.Date)
                continue;
            if (toDate.HasValue && item.Date > toDate.Value.Date)
                continue;

            ledger.Add(new LedgerEntry
            {
                Date = item.Date,
                VoucherType = item.VoucherType,
                VoucherNumber = item.VoucherNumber,
                Reference = item.Reference,
                DebitAmount = item.Debit,
                CreditAmount = item.Credit,
                RunningBalance = runningBalance,
                Notes = item.Notes
            });
        }

        return ledger;
    }
}
