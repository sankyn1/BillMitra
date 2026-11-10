using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<InventoryDashboardMetricsDto>>> GetMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _inventoryService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(ApiResponse<InventoryDashboardMetricsDto>.Ok(metrics));
    }

    [HttpGet("stock-summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductStockSummaryDto>>>> GetStockSummary(CancellationToken cancellationToken)
    {
        var summaries = await _inventoryService.GetStockSummariesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductStockSummaryDto>>.Ok(summaries));
    }

    [HttpGet("movements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockMovementDto>>>> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var movements = await _inventoryService.GetMovementsAsync(productId, warehouseId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StockMovementDto>>.Ok(movements));
    }

    [HttpGet("batches")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockItemBatchDto>>>> GetBatches(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var batches = await _inventoryService.GetBatchesAsync(productId, warehouseId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StockItemBatchDto>>.Ok(batches));
    }

    [HttpPost("batches")]
    public async Task<ActionResult<ApiResponse<StockItemBatchDto>>> CreateBatch(
        [FromBody] CreateStockBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty || string.IsNullOrWhiteSpace(request.BatchNumber))
        {
            return BadRequest(ApiResponse<StockItemBatchDto>.Fail("Valid Product ID and Batch Number are required."));
        }

        try
        {
            var batch = await _inventoryService.CreateBatchAsync(request, cancellationToken);
            return Ok(ApiResponse<StockItemBatchDto>.Ok(batch, "Batch registered successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<StockItemBatchDto>.Fail(ex.Message));
        }
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> RecordAdjustment(
        [FromBody] StockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty || request.WarehouseId == Guid.Empty)
        {
            return BadRequest(ApiResponse<StockAdjustmentDto>.Fail("Product ID and Warehouse ID are required."));
        }

        try
        {
            var result = await _inventoryService.RecordAdjustmentAsync(request, cancellationToken);
            return Ok(ApiResponse<StockAdjustmentDto>.Ok(result, "Physical stock adjustment reconciled."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<StockAdjustmentDto>.Fail(ex.Message));
        }
    }

    [HttpPost("transfers")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> RecordTransfer(
        [FromBody] InterWarehouseTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty || request.SourceWarehouseId == Guid.Empty || request.DestinationWarehouseId == Guid.Empty)
        {
            return BadRequest(ApiResponse<StockTransferDto>.Fail("Product, Source, and Destination Warehouse IDs are required."));
        }

        try
        {
            var result = await _inventoryService.RecordTransferAsync(request, cancellationToken);
            return Ok(ApiResponse<StockTransferDto>.Ok(result, "Stock transfer completed."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<StockTransferDto>.Fail(ex.Message));
        }
    }
}

