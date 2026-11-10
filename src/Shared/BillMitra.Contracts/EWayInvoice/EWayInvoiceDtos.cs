using System;

namespace BillMitra.Contracts.EWayInvoice;

public record GenerateEWayBillRequest(
    Guid SalesDocumentId,
    int TransportationMode,
    string? TransporterId,
    string? TransporterName,
    string? TransportDocNo,
    DateTime? TransportDocDate,
    string? VehicleNo,
    int VehicleType,
    int ApproximateDistanceKm
);

public record UpdatePartBRequest(
    string EWayBillNumber,
    string VehicleNo,
    string FromPlace,
    string FromStateCode,
    string ReasonCode,
    string? Remarks
);

public record CancelEWayBillRequest(
    string EWayBillNumber,
    string CancelReasonCode,
    string CancelRemarks
);

public record GenerateEInvoiceRequest(
    Guid SalesDocumentId
);

public record CancelEInvoiceRequest(
    string Irn,
    int CancelReason,
    string CancelRemarks
);

public record EWayBillDto(
    Guid Id,
    Guid SalesDocumentId,
    string EWayBillNumber,
    DateTimeOffset EWayBillDate,
    DateTimeOffset ValidUntil,
    int Status,
    string StatusName,
    string DocType,
    string DocNumber,
    DateTime DocDate,
    string FromGstin,
    string FromName,
    string ToGstin,
    string ToName,
    decimal TotalInvoiceValue,
    int TransportationMode,
    string? TransporterId,
    string? TransporterName,
    string? VehicleNo,
    int ApproximateDistanceKm,
    bool CanCancel,
    string? CancelReason,
    string? CancelRemarks,
    DateTimeOffset? CancelledAt
);

public record EInvoiceDto(
    Guid Id,
    Guid SalesDocumentId,
    string Irn,
    string AckNo,
    DateTimeOffset AckDate,
    string SignedQrCode,
    int Status,
    string StatusName,
    bool CanCancel,
    int? CancelReason,
    string? CancelRemarks,
    DateTimeOffset? CancelledAt
);

public record DocumentEWayInvoiceSummaryDto(
    Guid DocumentId,
    string DocumentNumber,
    decimal GrandTotal,
    bool IsEWayBillMandatory,
    bool IsEInvoiceEligible,
    EWayBillDto? EWayBill,
    EInvoiceDto? EInvoice
);
