using System;
using System.Collections.Generic;
using BillMitra.Domain.Enums;

namespace BillMitra.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount => Math.Max(0, Amount - AllocatedAmount);
    public string? ReferenceNumber { get; set; }
    public string? BankName { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? Notes { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public List<PaymentAllocation> Allocations { get; set; } = new();

    public void AddAllocation(Guid salesDocumentId, decimal allocatedAmount, string? notes = null)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentException("Allocated amount must be greater than zero.", nameof(allocatedAmount));

        if (AllocatedAmount + allocatedAmount > Amount)
            throw new InvalidOperationException($"Cannot allocate {allocatedAmount:N2}. Available unallocated amount is {UnallocatedAmount:N2}.");

        Allocations.Add(new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            PaymentId = Id,
            SalesDocumentId = salesDocumentId,
            AllocatedAmount = allocatedAmount,
            AllocationDate = PaymentDate,
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow
        });

        AllocatedAmount += allocatedAmount;
    }
}

public class PaymentAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public Guid SalesDocumentId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocationDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Additional display metadata
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public decimal InvoiceGrandTotal { get; set; }
    public decimal PreviouslyPaid { get; set; }
    public decimal OutstandingBeforeAllocation => Math.Max(0, InvoiceGrandTotal - PreviouslyPaid);
}

public class PaymentReversal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public DateTime ReversalDate { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public string? ReversedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class LedgerEntry
{
    public DateTime Date { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public string VoucherNumber { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? Notes { get; set; }
}

