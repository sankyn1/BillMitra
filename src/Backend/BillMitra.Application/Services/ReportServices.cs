using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Reports;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reportRepository,
        ICompanyRepository companyRepository,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<ComprehensiveReportsBundleDto> GetComprehensiveReportsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;

        var start = fromDate ?? DateTime.UtcNow.AddMonths(-1).Date;
        var end = toDate ?? DateTime.UtcNow.Date;

        var overview = await _reportRepository.GetFinancialOverviewAsync(companyId, start, end, cancellationToken);
        var trends = await _reportRepository.GetMonthlyFinancialTrendsAsync(companyId, 6, cancellationToken);
        var gstSummary = await _reportRepository.GetGstLiabilitySummaryAsync(companyId, start, end, cancellationToken);
        var agingBuckets = await _reportRepository.GetAgingBucketsAsync(companyId, cancellationToken);
        var overdueParties = await _reportRepository.GetTopOverduePartiesAsync(companyId, 10, cancellationToken);
        var velocities = await _reportRepository.GetProductMovementVelocitiesAsync(companyId, start, end, cancellationToken);

        return new ComprehensiveReportsBundleDto(
            Overview: overview,
            MonthlyTrends: trends.ToList(),
            GstSummary: gstSummary,
            AgingBuckets: agingBuckets.ToList(),
            TopOverdueParties: overdueParties.ToList(),
            ProductVelocities: velocities.ToList()
        );
    }

    public async Task<FinancialOverviewReportDto> GetFinancialOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;
        var start = fromDate ?? DateTime.UtcNow.AddMonths(-1).Date;
        var end = toDate ?? DateTime.UtcNow.Date;

        return await _reportRepository.GetFinancialOverviewAsync(companyId, start, end, cancellationToken);
    }

    public async Task<GstLiabilitySummaryDto> GetGstSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;
        var start = fromDate ?? DateTime.UtcNow.AddMonths(-1).Date;
        var end = toDate ?? DateTime.UtcNow.Date;

        return await _reportRepository.GetGstLiabilitySummaryAsync(companyId, start, end, cancellationToken);
    }

    public async Task<IReadOnlyList<AgingBucketDto>> GetAgingBucketsAsync(CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;
        return await _reportRepository.GetAgingBucketsAsync(companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductMovementVelocityDto>> GetProductVelocitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Empty;
        var start = fromDate ?? DateTime.UtcNow.AddMonths(-1).Date;
        var end = toDate ?? DateTime.UtcNow.Date;

        return await _reportRepository.GetProductMovementVelocitiesAsync(companyId, start, end, cancellationToken);
    }
}

