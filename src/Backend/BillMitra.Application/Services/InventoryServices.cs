using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Inventory;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<WarehouseService> _logger;

    public WarehouseService(
        IWarehouseRepository warehouseRepository,
        ICompanyRepository companyRepository,
        ILogger<WarehouseService> logger)
    {
        _warehouseRepository = warehouseRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WarehouseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;
        var list = await _warehouseRepository.ListAsync(companyId, cancellationToken);

        return list.Select(w => new WarehouseDto(
            w.Id,
            w.Name,
            w.Code,
            w.Address,
            w.City,
            w.StateCode,
            w.IsDefault,
            w.IsActive,
            w.CreatedAtUtc
        )).ToList();
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var w = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
        if (w == null) return null;

        return new WarehouseDto(
            w.Id,
            w.Name,
            w.Code,
            w.Address,
            w.City,
            w.StateCode,
            w.IsDefault,
            w.IsActive,
            w.CreatedAtUtc
        );
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;

        var entity = new Warehouse
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Address = request.Address,
            City = request.City,
            StateCode = request.StateCode,
            IsDefault = request.IsDefault,
            IsActive = true
        };

        var id = await _warehouseRepository.InsertAsync(entity, cancellationToken);
        entity.Id = id;

        _logger.LogInformation("Created new warehouse {Name} ({Code})", entity.Name, entity.Code);

        return new WarehouseDto(
            entity.Id,
            entity.Name,
            entity.Code,
            entity.Address,
            entity.City,
            entity.StateCode,
            entity.IsDefault,
            entity.IsActive,
            entity.CreatedAtUtc
        );
    }
}

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        ICompanyRepository companyRepository,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<InventoryDashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
    {
        var stockSummaries = await GetStockSummariesAsync(cancellationToken);
        var batches = await GetBatchesAsync(null, null, cancellationToken);
        var warehouses = await _warehouseRepository.ListAsync(Guid.Empty, cancellationToken);

        var totalValuation = stockSummaries.Sum(s => s.TotalStockValuation);
        var activeProducts = stockSummaries.Count;
        var lowStock = stockSummaries.Where(s => s.IsLowStock).ToList();
        var expiredBatchesCount = batches.Count(b => b.IsExpired);
        var expiringSoon = batches.Where(b => b.IsExpiringSoon && !b.IsExpired).ToList();

        return new InventoryDashboardMetricsDto(
            TotalStockValuation: totalValuation,
            TotalActiveProducts: activeProducts,
            LowStockAlertCount: lowStock.Count,
            ExpiredBatchesCount: expiredBatchesCount,
            WarehousesCount: warehouses.Count,
            LowStockItems: lowStock,
            ExpiringSoonBatches: expiringSoon
        );
    }

    public async Task<IReadOnlyList<ProductStockSummaryDto>> GetStockSummariesAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(null, null, cancellationToken);
        var batches = await _inventoryRepository.ListBatchesAsync(null, null, cancellationToken);

        var list = new List<ProductStockSummaryDto>();

        foreach (var p in products)
        {
            var productBatches = batches.Where(b => b.ProductId == p.Id).ToList();
            var totalQty = await _inventoryRepository.GetProductStockBalanceAsync(p.Id, null, cancellationToken);

            // If batches exist and have balance, we can also factor them
            var batchQty = productBatches.Sum(b => b.QuantityOnHand);
            var effectiveQty = Math.Max(totalQty, batchQty);

            var activeBatches = productBatches.Count(b => b.IsActive && (!b.ExpiryDate.HasValue || b.ExpiryDate.Value.Date >= DateTime.UtcNow.Date));
            var expiredBatches = productBatches.Count(b => b.IsExpired());

            var unitCost = p.PurchasePrice > 0 ? p.PurchasePrice : 0m;
            var valuation = effectiveQty * unitCost;
            var minReorder = p.MinStockAlert > 0 ? p.MinStockAlert : 10m;
            var isLowStock = effectiveQty <= minReorder;

            list.Add(new ProductStockSummaryDto(
                ProductId: p.Id,
                ProductName: p.Name,
                Sku: p.ItemCode ?? string.Empty,
                HsnCode: p.HsnSacCode,
                Unit: p.UnitCode,
                TotalStockQuantity: effectiveQty,
                PurchasePrice: p.PurchasePrice,
                SellingPrice: p.SalePrice,
                TotalStockValuation: valuation,
                MinStockAlert: minReorder,
                IsLowStock: isLowStock,
                ActiveBatchesCount: activeBatches,
                ExpiredBatchesCount: expiredBatches
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid? productId = null, Guid? warehouseId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var list = await _inventoryRepository.ListMovementsAsync(productId, warehouseId, fromDate, toDate, cancellationToken);
        return list.Select(m => new StockMovementDto(
            m.Id,
            m.ProductId,
            m.ProductName,
            m.WarehouseId,
            m.WarehouseName,
            m.BatchId,
            m.BatchNumber,
            (int)m.MovementType,
            m.MovementType.ToString(),
            m.Quantity,
            m.UnitCost,
            m.TotalCost,
            m.BalanceQuantityAfter,
            m.ReferenceDocumentNumber,
            m.ReferenceDocumentId,
            m.Remarks,
            m.MovementDate
        )).ToList();
    }

    public async Task<IReadOnlyList<StockItemBatchDto>> GetBatchesAsync(Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var batches = await _inventoryRepository.ListBatchesAsync(productId, warehouseId, cancellationToken);
        var products = (await _productRepository.GetAllAsync(null, null, cancellationToken)).ToDictionary(p => p.Id, p => p.Name);
        var warehouses = (await _warehouseRepository.ListAsync(Guid.Empty, cancellationToken)).ToDictionary(w => w.Id, w => w.Name);

        return batches.Select(b => new StockItemBatchDto(
            b.Id,
            b.ProductId,
            products.TryGetValue(b.ProductId, out var pName) ? pName : "Unknown Product",
            b.WarehouseId,
            warehouses.TryGetValue(b.WarehouseId, out var wName) ? wName : "Main Store",
            b.BatchNumber,
            b.ManufacturingDate,
            b.ExpiryDate,
            b.QuantityOnHand,
            b.PurchasePrice,
            b.Mrp,
            b.IsExpired(),
            b.IsExpiringSoon(),
            b.IsActive
        )).ToList();
    }

    public async Task<StockItemBatchDto> CreateBatchAsync(CreateStockBatchRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new ArgumentException("Product not found.");

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        var warehouseName = warehouse?.Name ?? "Main Store";

        var batch = new StockItemBatch
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            BatchNumber = request.BatchNumber.Trim(),
            ManufacturingDate = request.ManufacturingDate,
            ExpiryDate = request.ExpiryDate,
            QuantityOnHand = request.QuantityOnHand,
            PurchasePrice = request.PurchasePrice,
            Mrp = request.Mrp,
            IsActive = true
        };

        var id = await _inventoryRepository.InsertBatchAsync(batch, cancellationToken);
        batch.Id = id;

        // Record Initial Inward Movement if quantity > 0
        if (request.QuantityOnHand > 0)
        {
            var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
            var currentBalance = await _inventoryRepository.GetProductStockBalanceAsync(request.ProductId, request.WarehouseId, cancellationToken);
            var newBalance = currentBalance + request.QuantityOnHand;

            await _inventoryRepository.InsertMovementAsync(new StockMovementRecord
            {
                CompanyId = company?.Id ?? Guid.Empty,
                ProductId = request.ProductId,
                ProductName = product.Name,
                WarehouseId = request.WarehouseId,
                WarehouseName = warehouseName,
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                MovementType = StockMovementType.PurchaseInward,
                Quantity = request.QuantityOnHand,
                UnitCost = request.PurchasePrice,
                BalanceQuantityAfter = newBalance,
                Remarks = $"Batch {batch.BatchNumber} initial entry",
                MovementDate = DateTime.UtcNow
            }, cancellationToken);
        }

        return new StockItemBatchDto(
            batch.Id,
            batch.ProductId,
            product.Name,
            batch.WarehouseId,
            warehouseName,
            batch.BatchNumber,
            batch.ManufacturingDate,
            batch.ExpiryDate,
            batch.QuantityOnHand,
            batch.PurchasePrice,
            batch.Mrp,
            batch.IsExpired(),
            batch.IsExpiringSoon(),
            batch.IsActive
        );
    }

    public async Task<StockAdjustmentDto> RecordAdjustmentAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new ArgumentException("Product not found.");

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        var warehouseName = warehouse?.Name ?? "Main Store";

        var currentSystemQty = await _inventoryRepository.GetProductStockBalanceAsync(request.ProductId, request.WarehouseId, cancellationToken);
        var unitCost = product.PurchasePrice > 0 ? product.PurchasePrice : 0m;
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);

        string? batchNumber = null;
        if (request.BatchId.HasValue)
        {
            var batch = await _inventoryRepository.GetBatchByIdAsync(request.BatchId.Value, cancellationToken);
            batchNumber = batch?.BatchNumber;
        }

        var reason = (StockAdjustmentReason)request.Reason;
        var adjRecord = new StockAdjustmentRecord
        {
            CompanyId = company?.Id ?? Guid.Empty,
            ProductId = request.ProductId,
            ProductName = product.Name,
            WarehouseId = request.WarehouseId,
            WarehouseName = warehouseName,
            BatchId = request.BatchId,
            BatchNumber = batchNumber,
            Reason = reason,
            SystemQuantity = currentSystemQty,
            PhysicalQuantity = request.PhysicalQuantity,
            UnitCost = unitCost,
            Remarks = request.Remarks,
            AdjustmentDate = DateTime.UtcNow
        };

        var adjId = await _inventoryRepository.InsertAdjustmentAsync(adjRecord, cancellationToken);
        adjRecord.Id = adjId;

        // Post stock movement reflecting the differential
        var diffQty = request.PhysicalQuantity - currentSystemQty;
        if (diffQty != 0)
        {
            var movementType = diffQty > 0
                ? StockMovementType.AdjustmentAddition
                : StockMovementType.AdjustmentReduction;

            await _inventoryRepository.InsertMovementAsync(new StockMovementRecord
            {
                CompanyId = company?.Id ?? Guid.Empty,
                ProductId = request.ProductId,
                ProductName = product.Name,
                WarehouseId = request.WarehouseId,
                WarehouseName = warehouseName,
                BatchId = request.BatchId,
                BatchNumber = batchNumber,
                MovementType = movementType,
                Quantity = diffQty,
                UnitCost = unitCost,
                BalanceQuantityAfter = request.PhysicalQuantity,
                ReferenceDocumentNumber = $"ADJ-{adjId.ToString()[..8].ToUpperInvariant()}",
                ReferenceDocumentId = adjId,
                Remarks = request.Remarks ?? $"Stock adjustment: {reason}",
                MovementDate = DateTime.UtcNow
            }, cancellationToken);

            // Also update batch quantity if specified
            if (request.BatchId.HasValue)
            {
                await _inventoryRepository.UpdateBatchQuantityAsync(request.BatchId.Value, diffQty, cancellationToken);
            }
        }

        return new StockAdjustmentDto(
            adjRecord.Id,
            adjRecord.ProductId,
            adjRecord.ProductName,
            adjRecord.WarehouseId,
            adjRecord.WarehouseName,
            adjRecord.BatchId,
            adjRecord.BatchNumber,
            (int)adjRecord.Reason,
            adjRecord.Reason.ToString(),
            adjRecord.SystemQuantity,
            adjRecord.PhysicalQuantity,
            adjRecord.AdjustmentQuantity,
            adjRecord.UnitCost,
            adjRecord.ImpactValue,
            adjRecord.Remarks,
            adjRecord.AdjustmentDate
        );
    }

    public async Task<StockTransferDto> RecordTransferAsync(InterWarehouseTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
            throw new ArgumentException("Source and Destination warehouses must be different.");

        if (request.Quantity <= 0)
            throw new ArgumentException("Transfer quantity must be greater than zero.");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new ArgumentException("Product not found.");

        var sourceWh = await _warehouseRepository.GetByIdAsync(request.SourceWarehouseId, cancellationToken);
        var destWh = await _warehouseRepository.GetByIdAsync(request.DestinationWarehouseId, cancellationToken);

        var sourceWhName = sourceWh?.Name ?? "Source Warehouse";
        var destWhName = destWh?.Name ?? "Destination Warehouse";

        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var trfNumber = await _inventoryRepository.GetNextTransferNumberAsync(cancellationToken);

        var transfer = new StockTransferRecord
        {
            CompanyId = company?.Id ?? Guid.Empty,
            TransferNumber = trfNumber,
            SourceWarehouseId = request.SourceWarehouseId,
            SourceWarehouseName = sourceWhName,
            DestinationWarehouseId = request.DestinationWarehouseId,
            DestinationWarehouseName = destWhName,
            ProductId = request.ProductId,
            ProductName = product.Name,
            Quantity = request.Quantity,
            Remarks = request.Remarks,
            TransferDate = DateTime.UtcNow
        };

        var trfId = await _inventoryRepository.InsertTransferAsync(transfer, cancellationToken);
        transfer.Id = trfId;

        var unitCost = product.PurchasePrice > 0 ? product.PurchasePrice : 0m;

        // 1. Outward from Source
        var sourceBal = await _inventoryRepository.GetProductStockBalanceAsync(request.ProductId, request.SourceWarehouseId, cancellationToken);
        await _inventoryRepository.InsertMovementAsync(new StockMovementRecord
        {
            CompanyId = company?.Id ?? Guid.Empty,
            ProductId = request.ProductId,
            ProductName = product.Name,
            WarehouseId = request.SourceWarehouseId,
            WarehouseName = sourceWhName,
            MovementType = StockMovementType.TransferOut,
            Quantity = -request.Quantity,
            UnitCost = unitCost,
            BalanceQuantityAfter = sourceBal - request.Quantity,
            ReferenceDocumentNumber = trfNumber,
            ReferenceDocumentId = trfId,
            Remarks = $"Transferred to {destWhName}",
            MovementDate = DateTime.UtcNow
        }, cancellationToken);

        // 2. Inward to Destination
        var destBal = await _inventoryRepository.GetProductStockBalanceAsync(request.ProductId, request.DestinationWarehouseId, cancellationToken);
        await _inventoryRepository.InsertMovementAsync(new StockMovementRecord
        {
            CompanyId = company?.Id ?? Guid.Empty,
            ProductId = request.ProductId,
            ProductName = product.Name,
            WarehouseId = request.DestinationWarehouseId,
            WarehouseName = destWhName,
            MovementType = StockMovementType.TransferIn,
            Quantity = request.Quantity,
            UnitCost = unitCost,
            BalanceQuantityAfter = destBal + request.Quantity,
            ReferenceDocumentNumber = trfNumber,
            ReferenceDocumentId = trfId,
            Remarks = $"Received from {sourceWhName}",
            MovementDate = DateTime.UtcNow
        }, cancellationToken);

        return new StockTransferDto(
            transfer.Id,
            transfer.TransferNumber,
            transfer.SourceWarehouseId,
            transfer.SourceWarehouseName,
            transfer.DestinationWarehouseId,
            transfer.DestinationWarehouseName,
            transfer.ProductId,
            transfer.ProductName,
            transfer.Quantity,
            transfer.Remarks,
            transfer.TransferDate
        );
    }
}
