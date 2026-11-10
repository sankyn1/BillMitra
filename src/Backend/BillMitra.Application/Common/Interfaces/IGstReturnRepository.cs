using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;

namespace BillMitra.Application.Common.Interfaces;

public interface IGstReturnRepository
{
    Task<GstReturnFiling> InsertAsync(GstReturnFiling filing, CancellationToken ct = default);
    Task<GstReturnFiling> UpdateAsync(GstReturnFiling filing, CancellationToken ct = default);
    Task<GstReturnFiling?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GstReturnFiling?> GetLatestFilingAsync(Guid companyId, GstReturnType returnType, string taxPeriod, CancellationToken ct = default);
    Task<IReadOnlyList<GstReturnFiling>> GetFilingHistoryAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDocument>> GetFinalizedDocumentsForPeriodAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
