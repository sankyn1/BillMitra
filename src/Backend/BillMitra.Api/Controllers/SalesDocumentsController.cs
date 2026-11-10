using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Sales;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/sales")]
public class SalesDocumentsController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesDocumentsController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpPost("calculate")]
    public ActionResult<ApiResponse<CalculateGstResponse>> CalculateGst([FromBody] CalculateGstRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(ApiResponse<CalculateGstResponse>.Fail("At least one line item is required for tax calculation."));
        }

        var result = _salesService.CalculateGst(request);
        return Ok(ApiResponse<CalculateGstResponse>.Ok(result, "Tax calculation completed successfully."));
    }

    [HttpPost("draft")]
    public async Task<ActionResult<ApiResponse<SalesDocumentDto>>> CreateDraft(
        [FromBody] CreateOrUpdateSalesDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail("Customer or recipient name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.PlaceOfSupplyStateCode))
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail("Place of Supply State Code is mandatory for GST determination."));
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail("An invoice must contain at least one line item."));
        }

        try
        {
            var user = User?.Identity?.Name ?? "Admin";
            var result = await _salesService.CreateDraftAsync(request, user, cancellationToken);
            return StatusCode(201, ApiResponse<SalesDocumentDto>.Ok(result, "Sales draft created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpPut("draft/{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesDocumentDto>>> UpdateDraft(
        Guid id,
        [FromBody] CreateOrUpdateSalesDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail("Customer or recipient name is required."));
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail("An invoice must contain at least one line item."));
        }

        try
        {
            var user = User?.Identity?.Name ?? "Admin";
            var result = await _salesService.UpdateDraftAsync(id, request, user, cancellationToken);
            if (result == null)
            {
                return NotFound(ApiResponse<SalesDocumentDto>.Fail($"Document with ID {id} was not found.", statusCode: 404));
            }

            return Ok(ApiResponse<SalesDocumentDto>.Ok(result, "Sales draft updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesDocumentDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var doc = await _salesService.GetByIdAsync(id, cancellationToken);
        if (doc == null)
        {
            return NotFound(ApiResponse<SalesDocumentDto>.Fail($"Document with ID {id} was not found.", statusCode: 404));
        }

        return Ok(ApiResponse<SalesDocumentDto>.Ok(doc));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesDocumentDto>>>> GetDocuments(
        [FromQuery] DocumentType? documentType,
        [FromQuery] DocumentStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? partyId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var docs = await _salesService.GetDocumentsAsync(
            documentType,
            status,
            fromDate,
            toDate,
            partyId,
            search,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<SalesDocumentDto>>.Ok(docs));
    }

    [HttpPost("{id:guid}/finalize")]
    public async Task<ActionResult<ApiResponse<SalesDocumentDto>>> FinalizeDocument(
        Guid id,
        [FromBody] FinalizeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = User?.Identity?.Name ?? "Admin";
            var finalized = await _salesService.FinalizeDocumentAsync(id, request, user, cancellationToken);
            return Ok(ApiResponse<SalesDocumentDto>.Ok(finalized, $"Document finalized successfully. Statutory Number: {finalized.DocumentNumber}"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SalesDocumentDto>.Fail(ex.Message, statusCode: 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<SalesDocumentDto>>> CancelDocument(
        Guid id,
        [FromBody] CancelDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CancellationReason))
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail("A valid reason is required to cancel a finalized document."));
        }

        try
        {
            var user = User?.Identity?.Name ?? "Admin";
            var cancelled = await _salesService.CancelDocumentAsync(id, request, user, cancellationToken);
            return Ok(ApiResponse<SalesDocumentDto>.Ok(cancelled, $"Document {cancelled.DocumentNumber} cancelled successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SalesDocumentDto>.Fail(ex.Message, statusCode: 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SalesDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromQuery] string format = "a4",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await _salesService.GetByIdAsync(id, cancellationToken);
            if (doc == null)
            {
                return NotFound();
            }

            var pdfBytes = await _salesService.GeneratePdfAsync(id, format, cancellationToken);
            var safeNumber = (doc.DocumentNumber ?? doc.DraftNumber).Replace("/", "-");
            var fileName = $"{doc.DocumentType}_{safeNumber}_{format}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail($"Failed to generate PDF: {ex.Message}"));
        }
    }
}
