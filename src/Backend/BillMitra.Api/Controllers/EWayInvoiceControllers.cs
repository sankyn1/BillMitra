using System;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.EWayInvoice;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/ewaybill")]
public class EWayBillController : ControllerBase
{
    private readonly IEWayBillService _ewayBillService;

    public EWayBillController(IEWayBillService ewayBillService)
    {
        _ewayBillService = ewayBillService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<EWayBillDto>>> Generate([FromBody] GenerateEWayBillRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _ewayBillService.GenerateAsync(request, ct);
            return Ok(ApiResponse<EWayBillDto>.Ok(result, "E-Way Bill generated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EWayBillDto>.Fail(ex.Message));
        }
    }

    [HttpPost("update-vehicle")]
    public async Task<ActionResult<ApiResponse<EWayBillDto>>> UpdateVehicle([FromBody] UpdatePartBRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _ewayBillService.UpdateVehicleAsync(request, ct);
            return Ok(ApiResponse<EWayBillDto>.Ok(result, "Vehicle details updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EWayBillDto>.Fail(ex.Message));
        }
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<EWayBillDto>>> Cancel([FromBody] CancelEWayBillRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _ewayBillService.CancelAsync(request, ct);
            return Ok(ApiResponse<EWayBillDto>.Ok(result, "E-Way Bill cancelled successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EWayBillDto>.Fail(ex.Message));
        }
    }

    [HttpGet("document/{docId:guid}")]
    public async Task<ActionResult<ApiResponse<EWayBillDto?>>> GetByDocumentId(Guid docId, CancellationToken ct)
    {
        var result = await _ewayBillService.GetByDocumentIdAsync(docId, ct);
        return Ok(ApiResponse<EWayBillDto?>.Ok(result));
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EWayBillDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var list = await _ewayBillService.ListAsync(page, pageSize, ct);
        return Ok(ApiResponse<IReadOnlyList<EWayBillDto>>.Ok(list));
    }
}

[ApiController]
[Route("api/v1/einvoice")]
public class EInvoiceController : ControllerBase
{
    private readonly IEInvoiceService _einvoiceService;

    public EInvoiceController(IEInvoiceService einvoiceService)
    {
        _einvoiceService = einvoiceService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<EInvoiceDto>>> Generate([FromBody] GenerateEInvoiceRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _einvoiceService.GenerateAsync(request, ct);
            return Ok(ApiResponse<EInvoiceDto>.Ok(result, "E-Invoice (IRN) generated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EInvoiceDto>.Fail(ex.Message));
        }
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<EInvoiceDto>>> Cancel([FromBody] CancelEInvoiceRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _einvoiceService.CancelAsync(request, ct);
            return Ok(ApiResponse<EInvoiceDto>.Ok(result, "E-Invoice (IRN) cancelled successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EInvoiceDto>.Fail(ex.Message));
        }
    }

    [HttpGet("document/{docId:guid}")]
    public async Task<ActionResult<ApiResponse<EInvoiceDto?>>> GetByDocumentId(Guid docId, CancellationToken ct)
    {
        var result = await _einvoiceService.GetByDocumentIdAsync(docId, ct);
        return Ok(ApiResponse<EInvoiceDto?>.Ok(result));
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EInvoiceDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var list = await _einvoiceService.ListAsync(page, pageSize, ct);
        return Ok(ApiResponse<IReadOnlyList<EInvoiceDto>>.Ok(list));
    }

    [HttpGet("document/{docId:guid}/summary")]
    public async Task<ActionResult<ApiResponse<DocumentEWayInvoiceSummaryDto>>> GetDocumentSummary(Guid docId, CancellationToken ct)
    {
        try
        {
            var result = await _einvoiceService.GetDocumentSummaryAsync(docId, ct);
            return Ok(ApiResponse<DocumentEWayInvoiceSummaryDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DocumentEWayInvoiceSummaryDto>.Fail(ex.Message));
        }
    }
}
