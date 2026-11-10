using System;
using BillMitra.Domain.Enums;

namespace BillMitra.Domain.Entities;

public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StockItemBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Mrp { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsExpired() => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.UtcNow.Date;
    public bool IsExpiringSoon(int daysThreshold = 30) =>
        ExpiryDate.HasValue &&
        ExpiryDate.Value.Date >= DateTime.UtcNow.Date &&
        ExpiryDate.Value.Date <= DateTime.UtcNow.Date.AddDays(daysThreshold);
}

public class StockMovementRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; } // Positive for inward, negative for outward
    public decimal UnitCost { get; set; }
    public decimal TotalCost => Math.Abs(Quantity) * UnitCost;
    public decimal BalanceQuantityAfter { get; set; }
    public string? ReferenceDocumentNumber { get; set; } // Invoice #, Bill #, Adj #
    public Guid? ReferenceDocumentId { get; set; }
    public string? Remarks { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StockAdjustmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public StockAdjustmentReason Reason { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal AdjustmentQuantity => PhysicalQuantity - SystemQuantity;
    public decimal UnitCost { get; set; }
    public decimal ImpactValue => AdjustmentQuantity * UnitCost;
    public string? Remarks { get; set; }
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StockTransferRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string TransferNumber { get; set; } = string.Empty; // TRF/26-27/0001
    public Guid SourceWarehouseId { get; set; }
    public string SourceWarehouseName { get; set; } = string.Empty;
    public Guid DestinationWarehouseId { get; set; }
    public string DestinationWarehouseName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Remarks { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

