using System;
using System.Collections.Generic;
using BillMitra.Domain.Enums;

namespace BillMitra.Domain.Entities;

public class PurchaseDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierGstin { get; set; }
    public string? SupplierAddress { get; set; }
    public string SupplierStateCode { get; set; } = string.Empty;
    public string PlaceOfSupplyStateCode { get; set; } = string.Empty;
    public SupplyType SupplyType { get; set; } = SupplyType.IntraState;

    public PurchaseDocumentType DocumentType { get; set; } = PurchaseDocumentType.PurchaseInvoice;
    public PurchaseDocumentStatus DocumentStatus { get; set; } = PurchaseDocumentStatus.Draft;
    
    public string? PurchaseOrderNumber { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public DateTime SupplierInvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }

    public bool IsReverseCharge { get; set; }
    public ItcEligibility DefaultItcEligibility { get; set; } = ItcEligibility.EligibleOther;

    // Financial totals
    public decimal SubTotalTaxable { get; set; }
    public decimal CgstTotal { get; set; }
    public decimal SgstTotal { get; set; }
    public decimal IgstTotal { get; set; }
    public decimal CessTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal RoundOffAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingBalance => Math.Max(0, GrandTotal - PaidAmount);

    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }

    public List<PurchaseDocumentItem> Items { get; set; } = new();

    public bool CanFinalize() => DocumentStatus == PurchaseDocumentStatus.Draft;
    public bool CanCancel() => DocumentStatus == PurchaseDocumentStatus.Finalized && PaidAmount == 0;
}

public class PurchaseDocumentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseDocumentId { get; set; }
    public Guid? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "NOS";
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstRate { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstRate { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessRate { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public ItcEligibility ItcEligibility { get; set; } = ItcEligibility.EligibleOther;
}

public class ExpenseCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ExpenseEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public string ExpenseCategoryName { get; set; } = string.Empty;
    public string VoucherNumber { get; set; } = string.Empty; // EXP/26-27/0001
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;
    public decimal Amount { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount => Amount + GstAmount;
    public bool IsItcClaimable { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public string? ReferenceNumber { get; set; }
    public string? PayeeName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

