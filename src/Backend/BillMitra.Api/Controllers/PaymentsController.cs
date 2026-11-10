using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Payments;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> RecordPayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PartyId == Guid.Empty)
        {
            return BadRequest(ApiResponse<PaymentDto>.Fail("Valid Party ID is required."));
        }

        if (request.Amount <= 0)
        {
            return BadRequest(ApiResponse<PaymentDto>.Fail("Payment amount must be greater than zero."));
        }

        try
        {
            var result = await _paymentService.RecordPaymentAsync(request, cancellationToken);
            return Ok(ApiResponse<PaymentDto>.Ok(result, "Payment recorded successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PaymentDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PaymentDto>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PaymentDto>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentDto>>>> GetPayments(
        [FromQuery] PaymentType? type,
        [FromQuery] PaymentStatus? status,
        [FromQuery] Guid? partyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var list = await _paymentService.GetPaymentsAsync(type, status, partyId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PaymentDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPaymentById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id, cancellationToken);
        if (payment == null)
        {
            return NotFound(ApiResponse<PaymentDto>.Fail($"Payment {id} not found."));
        }

        return Ok(ApiResponse<PaymentDto>.Ok(payment));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<bool>>> ReversePayment(
        Guid id,
        [FromBody] ReversePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(ApiResponse<bool>.Fail("Reversal reason is mandatory."));
        }

        var success = await _paymentService.ReversePaymentAsync(id, request, cancellationToken);
        if (!success)
        {
            return BadRequest(ApiResponse<bool>.Fail("Payment could not be reversed. It may not exist or is already reversed."));
        }

        return Ok(ApiResponse<bool>.Ok(true, "Payment reversed and all document balances restored successfully."));
    }

    [HttpGet("parties/{partyId:guid}/open-invoices")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpenInvoiceForPaymentDto>>>> GetOpenInvoices(
        Guid partyId,
        CancellationToken cancellationToken)
    {
        var list = await _paymentService.GetOpenInvoicesAsync(partyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OpenInvoiceForPaymentDto>>.Ok(list));
    }

    [HttpGet("parties/{partyId:guid}/ledger")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerLedgerEntryDto>>>> GetPartyLedger(
        Guid partyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var list = await _paymentService.GetPartyLedgerAsync(partyId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerLedgerEntryDto>>.Ok(list));
    }

    [HttpGet("parties/{partyId:guid}/aging")]
    public async Task<ActionResult<ApiResponse<PartyAgingSummaryDto>>> GetPartyAging(
        Guid partyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _paymentService.GetPartyAgingSummaryAsync(partyId, cancellationToken);
            return Ok(ApiResponse<PartyAgingSummaryDto>.Ok(summary));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PartyAgingSummaryDto>.Fail(ex.Message));
        }
    }
}
