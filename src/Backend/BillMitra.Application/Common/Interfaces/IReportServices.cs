using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Reports;

namespace BillMitra.Application.Common.Interfaces;

public interface IReportService
{
    Task<ComprehensiveReportsBundleDto> GetComprehensiveReportsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<FinancialOverviewReportDto> GetFinancialOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<GstLiabilitySummaryDto> GetGstSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgingBucketDto>> GetAgingBucketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductMovementVelocityDto>> GetProductVelocitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}
