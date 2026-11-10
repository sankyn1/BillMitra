using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Payments;

namespace BillMitra.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> RecordPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(PaymentType? type = null, PaymentStatus? status = null, Guid? partyId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<bool> ReversePaymentAsync(Guid paymentId, ReversePaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpenInvoiceForPaymentDto>> GetOpenInvoicesAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerLedgerEntryDto>> GetPartyLedgerAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<PartyAgingSummaryDto> GetPartyAgingSummaryAsync(Guid partyId, CancellationToken cancellationToken = default);
}
