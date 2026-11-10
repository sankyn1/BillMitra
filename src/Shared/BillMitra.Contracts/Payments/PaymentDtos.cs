using System;
using System.Collections.Generic;

namespace BillMitra.Contracts.Payments;

public enum PaymentType
{
    Receipt = 1, // Inward (from customer)
    Payment = 2  // Outward (to supplier)
}

public enum PaymentMode
{
    Cash = 1,
    BankTransfer = 2,
    Cheque = 3,
    Upi = 4,
    CreditCard = 5,
    DebitCard = 6
}

public enum PaymentStatus
{
    Active = 1,
    Reversed = 2
}

public record PaymentAllocationDto(
    Guid Id,
    Guid PaymentId,
    Guid SalesDocumentId,
    string DocumentNumber,
    DateTime DocumentDate,
    decimal InvoiceGrandTotal,
    decimal PreviouslyPaid,
    decimal OutstandingBeforeAllocation,
    decimal AllocatedAmount,
    DateTime AllocationDate,
    string? Notes);

public record PaymentDto(
    Guid Id,
    string PaymentNumber,
    Guid PartyId,
    string PartyName,
    string? PartyGstin,
    PaymentType PaymentType,
    PaymentMode PaymentMode,
    DateTime PaymentDate,
    decimal Amount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    string? ReferenceNumber,
    string? BankName,
    DateTime? ChequeDate,
    string? Notes,
    PaymentStatus Status,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    List<PaymentAllocationDto> Allocations);

public record PaymentAllocationItemRequest(
    Guid SalesDocumentId,
    decimal AllocatedAmount,
    string? Notes = null);

public record CreatePaymentRequest(
    Guid PartyId,
    PaymentType PaymentType,
    PaymentMode PaymentMode,
    DateTime PaymentDate,
    decimal Amount,
    string? ReferenceNumber,
    string? BankName,
    DateTime? ChequeDate,
    string? Notes,
    List<PaymentAllocationItemRequest>? Allocations = null);

public record ReversePaymentRequest(
    string Reason);

public record OpenInvoiceForPaymentDto(
    Guid SalesDocumentId,
    string DocumentNumber,
    DateTime DocumentDate,
    DateTime? DueDate,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal OutstandingBalance);

public record CustomerLedgerEntryDto(
    DateTime Date,
    string VoucherType,
    string VoucherNumber,
    string? Reference,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal RunningBalance,
    string? Notes);

public record PartyAgingSummaryDto(
    Guid PartyId,
    string PartyName,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal CurrentOutstanding,
    decimal Days0To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus);

