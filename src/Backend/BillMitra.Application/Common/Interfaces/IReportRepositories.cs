using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Reports;

namespace BillMitra.Application.Common.Interfaces;

public interface IReportRepository
{
    Task<FinancialOverviewReportDto> GetFinancialOverviewAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyFinancialTrendDto>> GetMonthlyFinancialTrendsAsync(Guid companyId, int numberOfMonths, CancellationToken cancellationToken = default);
    Task<GstLiabilitySummaryDto> GetGstLiabilitySummaryAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgingBucketDto>> GetAgingBucketsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartyAgingDetailDto>> GetTopOverduePartiesAsync(Guid companyId, int topN = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductMovementVelocityDto>> GetProductMovementVelocitiesAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

