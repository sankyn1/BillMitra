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
[Route("api/v1/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseDto>>>> GetList(CancellationToken cancellationToken)
    {
        var list = await _warehouseService.ListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WarehouseDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, cancellationToken);
        if (warehouse == null)
        {
            return NotFound(ApiResponse<WarehouseDto>.Fail("Warehouse not found."));
        }

        return Ok(ApiResponse<WarehouseDto>.Ok(warehouse));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Create(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(ApiResponse<WarehouseDto>.Fail("Warehouse name and code are required."));
        }

        try
        {
            var result = await _warehouseService.CreateAsync(request, cancellationToken);
            return Ok(ApiResponse<WarehouseDto>.Ok(result, "Warehouse created successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<WarehouseDto>.Fail(ex.Message));
        }
    }
}

