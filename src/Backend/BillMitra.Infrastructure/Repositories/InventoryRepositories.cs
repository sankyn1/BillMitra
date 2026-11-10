using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WarehouseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Warehouse>> ListAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, company_id as CompanyId, name, code, address, city, state_code as StateCode,
                   is_default as IsDefault, is_active as IsActive, created_at_utc as CreatedAtUtc
            FROM warehouses
            ORDER BY is_default DESC, name ASC";

        var list = await conn.QueryAsync<Warehouse>(sql);
        return list.ToList();
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, company_id as CompanyId, name, code, address, city, state_code as StateCode,
                   is_default as IsDefault, is_active as IsActive, created_at_utc as CreatedAtUtc
            FROM warehouses
            WHERE id = @Id";

        return await conn.QuerySingleOrDefaultAsync<Warehouse>(sql, new { Id = id });
    }

    public async Task<Warehouse?> GetDefaultAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, company_id as CompanyId, name, code, address, city, state_code as StateCode,
                   is_default as IsDefault, is_active as IsActive, created_at_utc as CreatedAtUtc
            FROM warehouses
            WHERE is_default = 1
            LIMIT 1";

        try
        {
            return await conn.QuerySingleOrDefaultAsync<Warehouse>(sql);
        }
        catch
        {
            // SQL Server fallback without LIMIT
            var sqlServer = @"
                SELECT TOP 1 id, company_id as CompanyId, name, code, address, city, state_code as StateCode,
                       is_default as IsDefault, is_active as IsActive, created_at_utc as CreatedAtUtc
                FROM warehouses
                WHERE is_default = 1";
            return await conn.QuerySingleOrDefaultAsync<Warehouse>(sqlServer);
        }
    }

    public async Task<Guid> InsertAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (warehouse.Id == Guid.Empty) warehouse.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO warehouses (id, company_id, name, code, address, city, state_code, is_default, is_active, created_at_utc)
            VALUES (@Id, @CompanyId, @Name, @Code, @Address, @City, @StateCode, @IsDefault, @IsActive, @CreatedAtUtc)";

        await conn.ExecuteAsync(sql, warehouse);
        return warehouse.Id;
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE warehouses
            SET name = @Name, code = @Code, address = @Address, city = @City, state_code = @StateCode,
                is_default = @IsDefault, is_active = @IsActive
            WHERE id = @Id";

        await conn.ExecuteAsync(sql, warehouse);
    }
}

public class InventoryRepository : IInventoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public InventoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<StockItemBatch>> ListBatchesAsync(Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, product_id as ProductId, warehouse_id as WarehouseId, batch_number as BatchNumber,
                   manufacturing_date as ManufacturingDate, expiry_date as ExpiryDate,
                   quantity_on_hand as QuantityOnHand, purchase_price as PurchasePrice, mrp as Mrp,
                   is_active as IsActive, created_at_utc as CreatedAtUtc
            FROM stock_item_batches
            WHERE (@ProductId IS NULL OR product_id = @ProductId)
              AND (@WarehouseId IS NULL OR warehouse_id = @WarehouseId)
            ORDER BY expiry_date ASC, batch_number ASC";

        var list = await conn.QueryAsync<StockItemBatch>(sql, new { ProductId = productId, WarehouseId = warehouseId });
        return list.ToList();
    }

    public async Task<StockItemBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, product_id as ProductId, warehouse_id as WarehouseId, batch_number as BatchNumber,
                   manufacturing_date as ManufacturingDate, expiry_date as ExpiryDate,
                   quantity_on_hand as QuantityOnHand, purchase_price as PurchasePrice, mrp as Mrp,
                   is_active as IsActive, created_at_utc as CreatedAtUtc
            FROM stock_item_batches
            WHERE id = @Id";

        return await conn.QuerySingleOrDefaultAsync<StockItemBatch>(sql, new { Id = id });
    }

    public async Task<Guid> InsertBatchAsync(StockItemBatch batch, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (batch.Id == Guid.Empty) batch.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO stock_item_batches (id, product_id, warehouse_id, batch_number, manufacturing_date, expiry_date, quantity_on_hand, purchase_price, mrp, is_active, created_at_utc)
            VALUES (@Id, @ProductId, @WarehouseId, @BatchNumber, @ManufacturingDate, @ExpiryDate, @QuantityOnHand, @PurchasePrice, @Mrp, @IsActive, @CreatedAtUtc)";

        await conn.ExecuteAsync(sql, batch);
        return batch.Id;
    }

    public async Task UpdateBatchQuantityAsync(Guid batchId, decimal quantityDelta, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE stock_item_batches
            SET quantity_on_hand = quantity_on_hand + @Delta
            WHERE id = @BatchId";

        await conn.ExecuteAsync(sql, new { BatchId = batchId, Delta = quantityDelta });
    }

    public async Task<IReadOnlyList<StockMovementRecord>> ListMovementsAsync(Guid? productId = null, Guid? warehouseId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, company_id as CompanyId, product_id as ProductId, product_name as ProductName,
                   warehouse_id as WarehouseId, warehouse_name as WarehouseName, batch_id as BatchId,
                   batch_number as BatchNumber, movement_type as MovementType, quantity, unit_cost as UnitCost,
                   balance_quantity_after as BalanceQuantityAfter,
                   reference_document_number as ReferenceDocumentNumber,
                   reference_document_id as ReferenceDocumentId,
                   remarks, movement_date as MovementDate, created_at_utc as CreatedAtUtc
            FROM stock_movements
            WHERE (@ProductId IS NULL OR product_id = @ProductId)
              AND (@WarehouseId IS NULL OR warehouse_id = @WarehouseId)
              AND (@FromDate IS NULL OR movement_date >= @FromDate)
              AND (@ToDate IS NULL OR movement_date <= @ToDate)
            ORDER BY movement_date DESC, created_at_utc DESC";

        var list = await conn.QueryAsync<StockMovementRecord>(sql, new
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            FromDate = fromDate,
            ToDate = toDate
        });
        return list.ToList();
    }

    public async Task<Guid> InsertMovementAsync(StockMovementRecord movement, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (movement.Id == Guid.Empty) movement.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO stock_movements (id, company_id, product_id, product_name, warehouse_id, warehouse_name, batch_id, batch_number, movement_type, quantity, unit_cost, balance_quantity_after, reference_document_number, reference_document_id, remarks, movement_date, created_at_utc)
            VALUES (@Id, @CompanyId, @ProductId, @ProductName, @WarehouseId, @WarehouseName, @BatchId, @BatchNumber, @MovementType, @Quantity, @UnitCost, @BalanceQuantityAfter, @ReferenceDocumentNumber, @ReferenceDocumentId, @Remarks, @MovementDate, @CreatedAtUtc)";

        await conn.ExecuteAsync(sql, movement);
        return movement.Id;
    }

    public async Task<IReadOnlyList<StockAdjustmentRecord>> ListAdjustmentsAsync(Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, company_id as CompanyId, product_id as ProductId, product_name as ProductName,
                   warehouse_id as WarehouseId, warehouse_name as WarehouseName, batch_id as BatchId,
                   batch_number as BatchNumber, reason, system_quantity as SystemQuantity,
                   physical_quantity as PhysicalQuantity, unit_cost as UnitCost,
                   remarks, adjustment_date as AdjustmentDate, created_at_utc as CreatedAtUtc
            FROM stock_adjustments
            WHERE (@ProductId IS NULL OR product_id = @ProductId)
              AND (@WarehouseId IS NULL OR warehouse_id = @WarehouseId)
            ORDER BY adjustment_date DESC";

        var list = await conn.QueryAsync<StockAdjustmentRecord>(sql, new { ProductId = productId, WarehouseId = warehouseId });
        return list.ToList();
    }

    public async Task<Guid> InsertAdjustmentAsync(StockAdjustmentRecord adjustment, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (adjustment.Id == Guid.Empty) adjustment.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO stock_adjustments (id, company_id, product_id, product_name, warehouse_id, warehouse_name, batch_id, batch_number, reason, system_quantity, physical_quantity, adjustment_quantity, unit_cost, impact_value, remarks, adjustment_date, created_at_utc)
            VALUES (@Id, @CompanyId, @ProductId, @ProductName, @WarehouseId, @WarehouseName, @BatchId, @BatchNumber, @Reason, @SystemQuantity, @PhysicalQuantity, @AdjustmentQuantity, @UnitCost, @ImpactValue, @Remarks, @AdjustmentDate, @CreatedAtUtc)";

        await conn.ExecuteAsync(sql, adjustment);
        return adjustment.Id;
    }

    public async Task<IReadOnlyList<StockTransferRecord>> ListTransfersAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, company_id as CompanyId, transfer_number as TransferNumber,
                   source_warehouse_id as SourceWarehouseId, source_warehouse_name as SourceWarehouseName,
                   destination_warehouse_id as DestinationWarehouseId, destination_warehouse_name as DestinationWarehouseName,
                   product_id as ProductId, product_name as ProductName, quantity, remarks,
                   transfer_date as TransferDate, created_at_utc as CreatedAtUtc
            FROM stock_transfers
            ORDER BY transfer_date DESC";

        var list = await conn.QueryAsync<StockTransferRecord>(sql);
        return list.ToList();
    }

    public async Task<Guid> InsertTransferAsync(StockTransferRecord transfer, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (transfer.Id == Guid.Empty) transfer.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO stock_transfers (id, company_id, transfer_number, source_warehouse_id, source_warehouse_name, destination_warehouse_id, destination_warehouse_name, product_id, product_name, quantity, remarks, transfer_date, created_at_utc)
            VALUES (@Id, @CompanyId, @TransferNumber, @SourceWarehouseId, @SourceWarehouseName, @DestinationWarehouseId, @DestinationWarehouseName, @ProductId, @ProductName, @Quantity, @Remarks, @TransferDate, @CreatedAtUtc)";

        await conn.ExecuteAsync(sql, transfer);
        return transfer.Id;
    }

    public async Task<string> GetNextTransferNumberAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM stock_transfers");
        var nextSeq = count + 1;
        var year = DateTime.UtcNow.Year % 100;
        var nextYear = year + 1;
        return $"TRF/{year:D2}-{nextYear:D2}/{nextSeq:D4}";
    }

    public async Task<decimal> GetProductStockBalanceAsync(Guid productId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT COALESCE(SUM(quantity), 0)
            FROM stock_movements
            WHERE product_id = @ProductId
              AND (@WarehouseId IS NULL OR warehouse_id = @WarehouseId)";

        return await conn.ExecuteScalarAsync<decimal>(sql, new { ProductId = productId, WarehouseId = warehouseId });
    }
}

