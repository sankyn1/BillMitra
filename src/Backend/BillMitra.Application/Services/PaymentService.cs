using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Payments;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly ISalesDocumentRepository _salesDocRepo;
    private readonly IPartyRepository _partyRepo;
    private readonly IFinancialYearRepository _financialYearRepo;
    private readonly IDocumentSequenceRepository _sequenceRepo;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepo,
        ISalesDocumentRepository salesDocRepo,
        IPartyRepository partyRepo,
        IFinancialYearRepository financialYearRepo,
        IDocumentSequenceRepository sequenceRepo,
        ILogger<PaymentService> logger)
    {
        _paymentRepo = paymentRepo;
        _salesDocRepo = salesDocRepo;
        _partyRepo = partyRepo;
        _financialYearRepo = financialYearRepo;
        _sequenceRepo = sequenceRepo;
        _logger = logger;
    }

    public async Task<PaymentDto> RecordPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be strictly greater than zero.", nameof(request.Amount));

        var party = await _partyRepo.GetByIdAsync(request.PartyId, cancellationToken);
        if (party == null)
            throw new KeyNotFoundException($"Party with ID {request.PartyId} was not found.");

        var activeFy = await _financialYearRepo.GetActiveFinancialYearAsync(cancellationToken);
        if (activeFy == null)
            throw new InvalidOperationException("No active financial year configured.");

        // Allocate sequence number (e.g. ReceiptVoucher for Receipt, or generic document sequence)
        var domainDocType = request.PaymentType == BillMitra.Contracts.Payments.PaymentType.Receipt
            ? BillMitra.Domain.Enums.DocumentType.ReceiptVoucher
            : BillMitra.Domain.Enums.DocumentType.DebitNote; // or similar outward sequence

        var paymentNumber = await _sequenceRepo.AllocateNextNumberAsync(activeFy.Id, domainDocType, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            PartyId = party.Id,
            PartyName = party.Name,
            PaymentType = (BillMitra.Domain.Enums.PaymentType)(int)request.PaymentType,
            PaymentMode = (BillMitra.Domain.Enums.PaymentMode)(int)request.PaymentMode,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            BankName = request.BankName,
            ChequeDate = request.ChequeDate,
            Notes = request.Notes,
            Status = BillMitra.Domain.Enums.PaymentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (request.Allocations != null && request.Allocations.Count > 0)
        {
            var totalAllocated = request.Allocations.Sum(a => a.AllocatedAmount);
            if (totalAllocated > request.Amount)
            {
                throw new InvalidOperationException($"Total allocated amount ({totalAllocated:N2}) exceeds payment amount ({request.Amount:N2}).");
            }

            foreach (var allocReq in request.Allocations.Where(a => a.AllocatedAmount > 0))
            {
                var doc = await _salesDocRepo.GetByIdAsync(allocReq.SalesDocumentId, cancellationToken);
                if (doc == null)
                    throw new KeyNotFoundException($"Sales document {allocReq.SalesDocumentId} not found.");

                if (doc.PartyId != party.Id)
                    throw new InvalidOperationException($"Document {doc.DocumentNumber} does not belong to party {party.Name}.");

                var outstanding = Math.Max(0, doc.GrandTotal - doc.PaidAmount);
                if (allocReq.AllocatedAmount > outstanding)
                {
                    throw new InvalidOperationException($"Allocated amount {allocReq.AllocatedAmount:N2} exceeds invoice {doc.DocumentNumber} outstanding balance {outstanding:N2}.");
                }

                payment.AddAllocation(doc.Id, allocReq.AllocatedAmount, allocReq.Notes);
            }
        }

        var saved = await _paymentRepo.InsertPaymentWithAllocationsAsync(payment, cancellationToken);
        _logger.LogInformation("Recorded payment {PaymentNumber} for party {PartyName} with amount {Amount:N2}", saved.PaymentNumber, party.Name, saved.Amount);

        return MapToDto(saved, party.Gstin);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepo.GetByIdAsync(id, cancellationToken);
        if (payment == null) return null;

        var party = await _partyRepo.GetByIdAsync(payment.PartyId, cancellationToken);
        return MapToDto(payment, party?.Gstin);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(
        BillMitra.Contracts.Payments.PaymentType? type = null,
        BillMitra.Contracts.Payments.PaymentStatus? status = null,
        Guid? partyId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepo.GetAllAsync((int?)type, (int?)status, partyId, fromDate, toDate, cancellationToken);
        return payments.Select(p => MapToDto(p, null)).ToList();
    }

    public async Task<bool> ReversePaymentAsync(Guid paymentId, ReversePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reversal reason is mandatory.", nameof(request.Reason));

        var success = await _paymentRepo.ReversePaymentAsync(paymentId, request.Reason, "System User", cancellationToken);
        if (success)
        {
            _logger.LogInformation("Payment {PaymentId} reversed successfully. Reason: {Reason}", paymentId, request.Reason);
        }
        return success;
    }

    public async Task<IReadOnlyList<OpenInvoiceForPaymentDto>> GetOpenInvoicesAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var openDocs = await _paymentRepo.GetOpenInvoicesForPartyAsync(partyId, cancellationToken);
        return openDocs.Select(d => new OpenInvoiceForPaymentDto(
            d.Id,
            d.DocumentNumber ?? d.DraftNumber,
            d.DocumentDate,
            d.DueDate,
            d.GrandTotal,
            d.PaidAmount,
            Math.Max(0, d.GrandTotal - d.PaidAmount)
        )).ToList();
    }

    public async Task<IReadOnlyList<CustomerLedgerEntryDto>> GetPartyLedgerAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var entries = await _paymentRepo.GetPartyLedgerAsync(partyId, fromDate, toDate, cancellationToken);
        return entries.Select(e => new CustomerLedgerEntryDto(
            e.Date,
            e.VoucherType,
            e.VoucherNumber,
            e.Reference,
            e.DebitAmount,
            e.CreditAmount,
            e.RunningBalance,
            e.Notes
        )).ToList();
    }

    public async Task<PartyAgingSummaryDto> GetPartyAgingSummaryAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var party = await _partyRepo.GetByIdAsync(partyId, cancellationToken);
        if (party == null)
            throw new KeyNotFoundException($"Party with ID {partyId} not found.");

        var openInvoices = await _paymentRepo.GetOpenInvoicesForPartyAsync(partyId, cancellationToken);
        var allDocs = await _salesDocRepo.GetAllAsync(partyId: partyId, cancellationToken: cancellationToken);

        decimal totalInvoiced = allDocs.Where(d => d.DocumentStatus != BillMitra.Domain.Enums.DocumentStatus.Cancelled && d.DocumentStatus != BillMitra.Domain.Enums.DocumentStatus.Draft).Sum(d => d.GrandTotal);
        decimal totalPaid = allDocs.Sum(d => d.PaidAmount);
        decimal currentOutstanding = Math.Max(0, totalInvoiced - totalPaid);

        decimal days0To30 = 0;
        decimal days31To60 = 0;
        decimal days61To90 = 0;
        decimal days90Plus = 0;

        var today = DateTime.UtcNow.Date;

        foreach (var inv in openInvoices)
        {
            var balance = Math.Max(0, inv.GrandTotal - inv.PaidAmount);
            if (balance <= 0) continue;

            var ageDays = (today - inv.DocumentDate.Date).TotalDays;
            if (ageDays <= 30)
                days0To30 += balance;
            else if (ageDays <= 60)
                days31To60 += balance;
            else if (ageDays <= 90)
                days61To90 += balance;
            else
                days90Plus += balance;
        }

        return new PartyAgingSummaryDto(
            party.Id,
            party.Name,
            totalInvoiced,
            totalPaid,
            currentOutstanding,
            days0To30,
            days31To60,
            days61To90,
            days90Plus
        );
    }

    private static PaymentDto MapToDto(Payment payment, string? gstin)
    {
        return new PaymentDto(
            payment.Id,
            payment.PaymentNumber,
            payment.PartyId,
            payment.PartyName,
            gstin,
            (BillMitra.Contracts.Payments.PaymentType)(int)payment.PaymentType,
            (BillMitra.Contracts.Payments.PaymentMode)(int)payment.PaymentMode,
            payment.PaymentDate,
            payment.Amount,
            payment.AllocatedAmount,
            payment.UnallocatedAmount,
            payment.ReferenceNumber,
            payment.BankName,
            payment.ChequeDate,
            payment.Notes,
            (BillMitra.Contracts.Payments.PaymentStatus)(int)payment.Status,
            payment.CreatedAtUtc,
            payment.CreatedBy,
            payment.Allocations.Select(a => new PaymentAllocationDto(
                a.Id,
                a.PaymentId,
                a.SalesDocumentId,
                a.DocumentNumber ?? string.Empty,
                a.DocumentDate ?? DateTime.MinValue,
                a.InvoiceGrandTotal,
                a.PreviouslyPaid,
                a.OutstandingBeforeAllocation,
                a.AllocatedAmount,
                a.AllocationDate,
                a.Notes
            )).ToList()
        );
    }
}

