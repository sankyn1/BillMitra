using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Purchases;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/purchases")]
public class PurchaseController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseDocumentDto>>> Create(
        [FromBody] CreateOrUpdatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail("Supplier name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.SupplierInvoiceNumber))
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail("Supplier invoice number is required."));
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail("Purchase invoice must contain at least one item."));
        }

        try
        {
            var result = await _purchaseService.CreateDraftAsync(request, cancellationToken);
            return Ok(ApiResponse<PurchaseDocumentDto>.Ok(result, "Purchase invoice draft created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseDocumentDto>>> Update(
        Guid id,
        [FromBody] CreateOrUpdatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.UpdateDraftAsync(id, request, cancellationToken);
            return Ok(ApiResponse<PurchaseDocumentDto>.Ok(result, "Purchase invoice draft updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseDocumentDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<PurchaseDocumentDto>.Fail("Purchase invoice not found."));
        }

        return Ok(ApiResponse<PurchaseDocumentDto>.Ok(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PurchaseDocumentDto>>>> GetList(
        [FromQuery] int? documentType,
        [FromQuery] int? status,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var list = await _purchaseService.ListAsync(documentType, status, supplierId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PurchaseDocumentDto>>.Ok(list));
    }

    [HttpPost("{id:guid}/finalize")]
    public async Task<ActionResult<ApiResponse<PurchaseDocumentDto>>> Finalize(
        Guid id,
        [FromBody] FinalizePurchaseRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.FinalizeAsync(id, request ?? new FinalizePurchaseRequest(), cancellationToken);
            return Ok(ApiResponse<PurchaseDocumentDto>.Ok(result, "Purchase invoice finalized successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<PurchaseDocumentDto>>> Cancel(
        Guid id,
        [FromQuery] string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _purchaseService.CancelAsync(id, reason, cancellationToken);
            return Ok(ApiResponse<PurchaseDocumentDto>.Ok(result, "Purchase invoice cancelled."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PurchaseDocumentDto>.Fail(ex.Message));
        }
    }
}
