using System;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public record EWayBillGenerateResult(
    bool Success,
    string? EWayBillNumber,
    DateTimeOffset EWayBillDate,
    DateTimeOffset ValidUntil,
    string? ErrorMessage
);

public record EInvoiceGenerationContext(
    Guid DocumentId,
    string DocumentNumber,
    DateTime DocumentDate,
    string DocumentType,
    string SellerGstin,
    string SellerLegalName,
    string BuyerGstin,
    string BuyerLegalName,
    string PlaceOfSupply,
    decimal TotalTaxableAmount,
    decimal TotalCgst,
    decimal TotalSgst,
    decimal TotalIgst,
    decimal TotalCess,
    decimal GrandTotal
);

public record EInvoiceGenerateResult(
    bool Success,
    string? Irn,
    string? AckNo,
    DateTimeOffset AckDate,
    string? SignedInvoice,
    string? SignedQrCode,
    string? ErrorMessage
);

public interface IEWayBillProvider
{
    Task<EWayBillGenerateResult> GenerateEWayBillAsync(EWayBillRecord record, CancellationToken cancellationToken = default);
    Task<bool> UpdateVehicleAsync(string ewayBillNumber, string vehicleNo, string fromPlace, string fromState, string reason, string? remarks, CancellationToken cancellationToken = default);
    Task<bool> CancelEWayBillAsync(string ewayBillNumber, string reasonCode, string remarks, CancellationToken cancellationToken = default);
}

public interface IEInvoiceProvider
{
    Task<EInvoiceGenerateResult> GenerateIrnAsync(EInvoiceGenerationContext context, CancellationToken cancellationToken = default);
    Task<bool> CancelIrnAsync(string irn, int cancelReason, string remarks, CancellationToken cancellationToken = default);
}

