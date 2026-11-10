using System;
using BillMitra.Domain.Enums;

namespace BillMitra.Domain.Entities;

public class EWayBillRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesDocumentId { get; set; }
    public string EWayBillNumber { get; set; } = string.Empty;
    public DateTimeOffset EWayBillDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ValidUntil { get; set; }
    public EWayBillStatus Status { get; set; } = EWayBillStatus.Generated;
    public SupplyType SupplyType { get; set; } = SupplyType.IntraState;
    public EWayBillSubSupplyType SubSupplyType { get; set; } = EWayBillSubSupplyType.Supply;
    
    public string DocType { get; set; } = "INV";
    public string DocNumber { get; set; } = string.Empty;
    public DateTime DocDate { get; set; } = DateTime.UtcNow.Date;

    // Supplier / Origin
    public string FromGstin { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromPlace { get; set; } = string.Empty;
    public string FromPincode { get; set; } = string.Empty;
    public string FromStateCode { get; set; } = string.Empty;

    // Recipient / Destination
    public string ToGstin { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string ToPlace { get; set; } = string.Empty;
    public string ToPincode { get; set; } = string.Empty;
    public string ToStateCode { get; set; } = string.Empty;

    // Consignment Values
    public decimal TotalInvoiceValue { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessAmount { get; set; }

    // Part-B: Transportation
    public TransportationMode TransportationMode { get; set; } = TransportationMode.Road;
    public string? TransporterId { get; set; }
    public string? TransporterName { get; set; }
    public string? TransportDocNo { get; set; }
    public DateTime? TransportDocDate { get; set; }
    public string? VehicleNo { get; set; }
    public VehicleType VehicleType { get; set; } = VehicleType.Regular;
    public int ApproximateDistanceKm { get; set; } = 10;

    // Cancellation
    public string? CancelReason { get; set; }
    public string? CancelRemarks { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool CanCancel() => Status != EWayBillStatus.Cancelled && (DateTimeOffset.UtcNow - EWayBillDate).TotalHours <= 24;
}

public class EInvoiceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesDocumentId { get; set; }
    public string Irn { get; set; } = string.Empty; // 64-character hash
    public string AckNo { get; set; } = string.Empty; // 15-character statutory ack
    public DateTimeOffset AckDate { get; set; } = DateTimeOffset.UtcNow;
    
    public string SignedInvoice { get; set; } = string.Empty;
    public string SignedQrCode { get; set; } = string.Empty;
    
    public EInvoiceStatus Status { get; set; } = EInvoiceStatus.Generated;
    public EInvoiceCancelReason? CancelReason { get; set; }
    public string? CancelRemarks { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool CanCancel() => Status == EInvoiceStatus.Generated && (DateTimeOffset.UtcNow - AckDate).TotalHours <= 24;
}

