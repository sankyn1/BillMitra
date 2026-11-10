using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Gst;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GstController : ControllerBase
{
    private readonly IGstReturnService _gstService;

    public GstController(IGstReturnService gstService)
    {
        _gstService = gstService;
    }

    [HttpGet("gstr1/summary")]
    public async Task<ActionResult<ApiResponse<Gstr1SummaryDto>>> GetGstr1Summary(
        [FromQuery] string? taxPeriod,
        CancellationToken cancellationToken)
    {
        var period = taxPeriod ?? $"{DateTime.Today.Month:D2}{DateTime.Today.Year}";
        try
        {
            var summary = await _gstService.GetGstr1SummaryAsync(period, cancellationToken);
            return Ok(ApiResponse<Gstr1SummaryDto>.Ok(summary));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<Gstr1SummaryDto>.Fail(ex.Message));
        }
    }

    [HttpGet("gstr1/validate")]
    public async Task<ActionResult<ApiResponse<GstValidationResultDto>>> ValidateGstr1(
        [FromQuery] string? taxPeriod,
        CancellationToken cancellationToken)
    {
        var period = taxPeriod ?? $"{DateTime.Today.Month:D2}{DateTime.Today.Year}";
        var result = await _gstService.ValidateGstr1Async(period, cancellationToken);
        return Ok(ApiResponse<GstValidationResultDto>.Ok(result));
    }

    [HttpGet("gstr1/json")]
    public async Task<IActionResult> DownloadGstr1Json(
        [FromQuery] string? taxPeriod,
        CancellationToken cancellationToken)
    {
        var period = taxPeriod ?? $"{DateTime.Today.Month:D2}{DateTime.Today.Year}";
        try
        {
            var jsonRoot = await _gstService.GenerateGstr1JsonPayloadAsync(period, cancellationToken);
            var jsonString = JsonSerializer.Serialize(jsonRoot, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(jsonString);
            var fileName = $"GSTR1_{jsonRoot.gstin}_{period}.json";

            return File(bytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail($"Failed to generate GSTR-1 JSON: {ex.Message}"));
        }
    }

    [HttpGet("gstr3b/summary")]
    public async Task<ActionResult<ApiResponse<Gstr3bSummaryDto>>> GetGstr3bSummary(
        [FromQuery] string? taxPeriod,
        CancellationToken cancellationToken)
    {
        var period = taxPeriod ?? $"{DateTime.Today.Month:D2}{DateTime.Today.Year}";
        try
        {
            var summary = await _gstService.GetGstr3bSummaryAsync(period, cancellationToken);
            return Ok(ApiResponse<Gstr3bSummaryDto>.Ok(summary));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<Gstr3bSummaryDto>.Fail(ex.Message));
        }
    }

    [HttpGet("hsn-summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HsnSummaryItemDto>>>> GetHsnSummary(
        [FromQuery] string? taxPeriod,
        CancellationToken cancellationToken)
    {
        var period = taxPeriod ?? $"{DateTime.Today.Month:D2}{DateTime.Today.Year}";
        try
        {
            var items = await _gstService.GetHsnSummaryAsync(period, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<HsnSummaryItemDto>>.Ok(items));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<HsnSummaryItemDto>>.Fail(ex.Message));
        }
    }

    [HttpGet("filings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GstReturnFilingDto>>>> GetFilingHistory(
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await _gstService.GetFilingHistoryAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<GstReturnFilingDto>>.Ok(history));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<GstReturnFilingDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("gstr1/submit")]
    public async Task<ActionResult<ApiResponse<GstReturnFilingDto>>> SubmitGstr1(
        [FromBody] SubmitGstReturnRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var filing = await _gstService.SubmitGstr1Async(request.TaxPeriod, request.SimulateProvider, cancellationToken);
            if (filing.FilingStatus == GstFilingStatusDto.ValidationFailed)
            {
                return BadRequest(ApiResponse<GstReturnFilingDto>.Fail("GSTR-1 validation failed. Resolve statutory errors before submitting.", statusCode: 400));
            }

            var msg = filing.FilingStatus == GstFilingStatusDto.Filed
                ? $"GSTR-1 successfully submitted and acknowledged. ARN: {filing.Arn}"
                : "GSTR-1 submission sent to provider.";

            return Ok(ApiResponse<GstReturnFilingDto>.Ok(filing, msg));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<GstReturnFilingDto>.Fail($"Submission failed: {ex.Message}"));
        }
    }

    [HttpPost("filings/{id:guid}/mark-filed")]
    public async Task<ActionResult<ApiResponse<GstReturnFilingDto>>> MarkAsFiled(
        Guid id,
        [FromBody] MarkReturnFiledRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Arn))
        {
            return BadRequest(ApiResponse<GstReturnFilingDto>.Fail("A valid Application Reference Number (ARN) is required."));
        }

        try
        {
            var filing = await _gstService.MarkAsFiledAsync(id, request.Arn, request.ArnDate, cancellationToken);
            return Ok(ApiResponse<GstReturnFilingDto>.Ok(filing, $"Filing record updated with ARN: {filing.Arn}"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<GstReturnFilingDto>.Fail(ex.Message, statusCode: 404));
        }
    }
}

