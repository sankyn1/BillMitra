using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Reports;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("comprehensive")]
    public async Task<ActionResult<ApiResponse<ComprehensiveReportsBundleDto>>> GetComprehensive(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var bundle = await _reportService.GetComprehensiveReportsAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<ComprehensiveReportsBundleDto>.Ok(bundle));
    }

    [HttpGet("financial-overview")]
    public async Task<ActionResult<ApiResponse<FinancialOverviewReportDto>>> GetOverview(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var overview = await _reportService.GetFinancialOverviewAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<FinancialOverviewReportDto>.Ok(overview));
    }

    [HttpGet("gst-summary")]
    public async Task<ActionResult<ApiResponse<GstLiabilitySummaryDto>>> GetGstSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var gstSummary = await _reportService.GetGstSummaryAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<GstLiabilitySummaryDto>.Ok(gstSummary));
    }

    [HttpGet("aging-buckets")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgingBucketDto>>>> GetAgingBuckets(CancellationToken cancellationToken)
    {
        var buckets = await _reportService.GetAgingBucketsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AgingBucketDto>>.Ok(buckets));
    }

    [HttpGet("product-velocity")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductMovementVelocityDto>>>> GetProductVelocity(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var velocities = await _reportService.GetProductVelocitiesAsync(fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductMovementVelocityDto>>.Ok(velocities));
    }
}
