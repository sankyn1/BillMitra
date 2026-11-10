using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Inventory;

namespace BillMitra.Application.Common.Interfaces;

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task<InventoryDashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductStockSummaryDto>> GetStockSummariesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid? productId = null, Guid? warehouseId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItemBatchDto>> GetBatchesAsync(Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<StockItemBatchDto> CreateBatchAsync(CreateStockBatchRequest request, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> RecordAdjustmentAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto> RecordTransferAsync(InterWarehouseTransferRequest request, CancellationToken cancellationToken = default);
}

