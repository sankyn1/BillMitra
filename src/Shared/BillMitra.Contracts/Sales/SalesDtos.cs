using System;
using System.Collections.Generic;

namespace BillMitra.Contracts.Sales;

public enum DocumentType
{
    TaxInvoice = 1,
    BillOfSupply = 2,
    Quotation = 3,
    ProformaInvoice = 4,
    CreditNote = 5,
    DebitNote = 6,
    DeliveryChallan = 7
}

public enum DocumentStatus
{
    Draft = 1,
    Finalized = 2,
    Cancelled = 3
}

public enum GstSupplyType
{
    IntraState = 1, // CGST + SGST
    InterState = 2  // IGST
}

public sealed record SalesDocumentItemDto(
    Guid Id,
    Guid? ProductId,
    string ItemName,
    string HsnSacCode,
    decimal Quantity,
    string UnitCode,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal GstRatePercent,
    decimal CgstRatePercent,
    decimal CgstAmount,
    decimal SgstRatePercent,
    decimal SgstAmount,
    decimal IgstRatePercent,
    decimal IgstAmount,
    decimal CessRatePercent,
    decimal CessAmount,
    decimal TotalAmount
);

public sealed record SalesDocumentDto(
    Guid Id,
    DocumentType DocumentType,
    DocumentStatus DocumentStatus,
    string? DocumentNumber,
    string DraftNumber,
    DateTime DocumentDate,
    DateTime? DueDate,
    Guid CompanyId,
    Guid? PartyId,
    string CustomerName,
    string? CustomerGstin,
    string? BillingAddress,
    string? ShippingAddress,
    string PlaceOfSupplyStateCode,
    string SupplierStateCode,
    bool IsReverseCharge,
    GstSupplyType SupplyType,
    decimal SubTotalTaxable,
    decimal CgstTotal,
    decimal SgstTotal,
    decimal IgstTotal,
    decimal CessTotal,
    decimal TotalTax,
    decimal RoundOffAmount,
    decimal GrandTotal,
    string TotalAmountInWords,
    string? Notes,
    string? TermsAndConditions,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    IReadOnlyList<SalesDocumentItemDto> Items
);

public sealed record CreateOrUpdateSalesDocumentRequest(
    DocumentType DocumentType,
    DateTime DocumentDate,
    DateTime? DueDate,
    Guid? PartyId,
    string CustomerName,
    string? CustomerGstin,
    string? BillingAddress,
    string? ShippingAddress,
    string PlaceOfSupplyStateCode,
    bool IsReverseCharge,
    string? Notes,
    string? TermsAndConditions,
    List<SalesDocumentItemInput> Items
);

public sealed record SalesDocumentItemInput(
    Guid? ProductId,
    string ItemName,
    string HsnSacCode,
    decimal Quantity,
    string UnitCode,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal GstRatePercent,
    decimal CessRatePercent = 0
);

public sealed record CalculateGstRequest(
    string SupplierStateCode,
    string PlaceOfSupplyStateCode,
    bool IsReverseCharge,
    List<SalesDocumentItemInput> Items
);

public sealed record CalculateGstResponse(
    GstSupplyType SupplyType,
    decimal SubTotalTaxable,
    decimal CgstTotal,
    decimal SgstTotal,
    decimal IgstTotal,
    decimal CessTotal,
    decimal TotalTax,
    decimal RoundOffAmount,
    decimal GrandTotal,
    string TotalAmountInWords,
    IReadOnlyList<CalculatedItemResponse> Items
);

public sealed record CalculatedItemResponse(
    int LineIndex,
    string ItemName,
    string HsnSacCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal GstRatePercent,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal CessAmount,
    decimal TotalAmount
);

public sealed record FinalizeDocumentRequest(
    string? IdempotencyKey
);

public sealed record CancelDocumentRequest(
    string CancellationReason
);
