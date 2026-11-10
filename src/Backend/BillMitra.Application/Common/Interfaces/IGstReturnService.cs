using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Gst;

namespace BillMitra.Application.Common.Interfaces;

public interface IGstReturnService
{
    Task<Gstr1SummaryDto> GetGstr1SummaryAsync(string taxPeriod, CancellationToken ct = default);
    Task<Gstr1JsonRoot> GenerateGstr1JsonPayloadAsync(string taxPeriod, CancellationToken ct = default);
    Task<GstValidationResultDto> ValidateGstr1Async(string taxPeriod, CancellationToken ct = default);
    Task<Gstr3bSummaryDto> GetGstr3bSummaryAsync(string taxPeriod, CancellationToken ct = default);
    Task<IReadOnlyList<HsnSummaryItemDto>> GetHsnSummaryAsync(string taxPeriod, CancellationToken ct = default);
    Task<IReadOnlyList<GstReturnFilingDto>> GetFilingHistoryAsync(CancellationToken ct = default);
    Task<GstReturnFilingDto> SubmitGstr1Async(string taxPeriod, bool simulate = true, CancellationToken ct = default);
    Task<GstReturnFilingDto> MarkAsFiledAsync(Guid filingId, string arn, DateTime arnDate, CancellationToken ct = default);
}
