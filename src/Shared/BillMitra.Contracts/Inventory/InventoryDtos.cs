using System;
using System.Collections.Generic;

namespace BillMitra.Contracts.Inventory;

public record WarehouseDto(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    string? City,
    string? StateCode,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record CreateWarehouseRequest(
    string Name,
    string Code,
    string? Address,
    string? City,
    string? StateCode,
    bool IsDefault = false
);

public record StockItemBatchDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    string BatchNumber,
    DateTime? ManufacturingDate,
    DateTime? ExpiryDate,
    decimal QuantityOnHand,
    decimal PurchasePrice,
    decimal Mrp,
    bool IsExpired,
    bool IsExpiringSoon,
    bool IsActive
);

public record CreateStockBatchRequest(
    Guid ProductId,
    Guid WarehouseId,
    string BatchNumber,
    DateTime? ManufacturingDate,
    DateTime? ExpiryDate,
    decimal QuantityOnHand,
    decimal PurchasePrice,
    decimal Mrp
);

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    Guid? BatchId,
    string? BatchNumber,
    int MovementType,
    string MovementTypeName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    decimal BalanceQuantityAfter,
    string? ReferenceDocumentNumber,
    Guid? ReferenceDocumentId,
    string? Remarks,
    DateTime MovementDate
);

public record StockAdjustmentRequest(
    Guid ProductId,
    Guid WarehouseId,
    Guid? BatchId,
    int Reason,
    decimal PhysicalQuantity,
    string? Remarks
);

public record StockAdjustmentDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    Guid? BatchId,
    string? BatchNumber,
    int Reason,
    string ReasonName,
    decimal SystemQuantity,
    decimal PhysicalQuantity,
    decimal AdjustmentQuantity,
    decimal UnitCost,
    decimal ImpactValue,
    string? Remarks,
    DateTime AdjustmentDate
);

public record InterWarehouseTransferRequest(
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    Guid ProductId,
    decimal Quantity,
    string? Remarks
);

public record StockTransferDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string SourceWarehouseName,
    Guid DestinationWarehouseId,
    string DestinationWarehouseName,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    string? Remarks,
    DateTime TransferDate
);

public record ProductStockSummaryDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    string HsnCode,
    string Unit,
    decimal TotalStockQuantity,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal TotalStockValuation,
    decimal MinStockAlert,
    bool IsLowStock,
    int ActiveBatchesCount,
    int ExpiredBatchesCount
);

public record InventoryDashboardMetricsDto(
    decimal TotalStockValuation,
    int TotalActiveProducts,
    int LowStockAlertCount,
    int ExpiredBatchesCount,
    int WarehousesCount,
    List<ProductStockSummaryDto> LowStockItems,
    List<StockItemBatchDto> ExpiringSoonBatches
);

