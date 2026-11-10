using System;
using System.Collections.Generic;

namespace BillMitra.Contracts.Purchases;

public record PurchaseDocumentItemInput(
    Guid? ProductId,
    string ItemName,
    string HsnCode,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal GstRate,
    int ItcEligibility = 1
);

public record CreateOrUpdatePurchaseRequest(
    int DocumentType,
    Guid? SupplierId,
    string SupplierName,
    string? SupplierGstin,
    string? SupplierAddress,
    string SupplierStateCode,
    string PlaceOfSupplyStateCode,
    string SupplierInvoiceNumber,
    DateTime SupplierInvoiceDate,
    DateTime DocumentDate,
    DateTime? DueDate,
    bool IsReverseCharge,
    int DefaultItcEligibility,
    string? Notes,
    List<PurchaseDocumentItemInput> Items
);

public record FinalizePurchaseRequest(
    string? Notes = null
);

public record PurchaseDocumentItemDto(
    Guid Id,
    Guid? ProductId,
    string ItemName,
    string HsnCode,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal GstRate,
    decimal TaxableAmount,
    decimal CgstRate,
    decimal CgstAmount,
    decimal SgstRate,
    decimal SgstAmount,
    decimal IgstRate,
    decimal IgstAmount,
    decimal CessRate,
    decimal CessAmount,
    decimal TotalAmount,
    int ItcEligibility,
    string ItcEligibilityName
);

public record PurchaseDocumentDto(
    Guid Id,
    int DocumentType,
    string DocumentTypeName,
    int DocumentStatus,
    string DocumentStatusName,
    Guid? SupplierId,
    string SupplierName,
    string? SupplierGstin,
    string? SupplierAddress,
    string SupplierStateCode,
    string PlaceOfSupplyStateCode,
    string SupplierInvoiceNumber,
    DateTime SupplierInvoiceDate,
    DateTime DocumentDate,
    DateTime? DueDate,
    bool IsReverseCharge,
    int DefaultItcEligibility,
    decimal SubTotalTaxable,
    decimal CgstTotal,
    decimal SgstTotal,
    decimal IgstTotal,
    decimal CessTotal,
    decimal TotalTax,
    decimal RoundOffAmount,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal OutstandingBalance,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc,
    List<PurchaseDocumentItemDto> Items
);

public record CreateExpenseRequest(
    Guid ExpenseCategoryId,
    DateTime ExpenseDate,
    decimal Amount,
    decimal GstRate,
    bool IsItcClaimable,
    int PaymentMode,
    string? ReferenceNumber,
    string? PayeeName,
    string? Notes
);

public record ExpenseCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive
);

public record ExpenseDto(
    Guid Id,
    Guid ExpenseCategoryId,
    string ExpenseCategoryName,
    string VoucherNumber,
    DateTime ExpenseDate,
    decimal Amount,
    decimal GstRate,
    decimal GstAmount,
    decimal TotalAmount,
    bool IsItcClaimable,
    int PaymentMode,
    string PaymentModeName,
    string? ReferenceNumber,
    string? PayeeName,
    string? Notes,
    DateTime CreatedAtUtc
);
