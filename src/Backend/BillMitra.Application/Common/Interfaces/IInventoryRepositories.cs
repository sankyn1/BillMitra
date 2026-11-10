using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> ListAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetDefaultAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
}

public interface IInventoryRepository
{
    // Batches
    Task<IReadOnlyList<StockItemBatch>> ListBatchesAsync(Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<StockItemBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> InsertBatchAsync(StockItemBatch batch, CancellationToken cancellationToken = default);
    Task UpdateBatchQuantityAsync(Guid batchId, decimal quantityDelta, CancellationToken cancellationToken = default);

    // Stock Movements
    Task<IReadOnlyList<StockMovementRecord>> ListMovementsAsync(Guid? productId = null, Guid? warehouseId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Guid> InsertMovementAsync(StockMovementRecord movement, CancellationToken cancellationToken = default);

    // Stock Adjustments
    Task<IReadOnlyList<StockAdjustmentRecord>> ListAdjustmentsAsync(Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<Guid> InsertAdjustmentAsync(StockAdjustmentRecord adjustment, CancellationToken cancellationToken = default);

    // Stock Transfers
    Task<IReadOnlyList<StockTransferRecord>> ListTransfersAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Guid> InsertTransferAsync(StockTransferRecord transfer, CancellationToken cancellationToken = default);
    Task<string> GetNextTransferNumberAsync(CancellationToken cancellationToken = default);

    // Stock Balances
    Task<decimal> GetProductStockBalanceAsync(Guid productId, Guid? warehouseId = null, CancellationToken cancellationToken = default);
}

