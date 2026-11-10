using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.EWayInvoice;

namespace BillMitra.Application.Common.Interfaces;

public interface IEWayBillService
{
    Task<EWayBillDto> GenerateAsync(GenerateEWayBillRequest request, CancellationToken cancellationToken = default);
    Task<EWayBillDto> UpdateVehicleAsync(UpdatePartBRequest request, CancellationToken cancellationToken = default);
    Task<EWayBillDto> CancelAsync(CancelEWayBillRequest request, CancellationToken cancellationToken = default);
    Task<EWayBillDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EWayBillDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}

public interface IEInvoiceService
{
    Task<EInvoiceDto> GenerateAsync(GenerateEInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<EInvoiceDto> CancelAsync(CancelEInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<EInvoiceDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EInvoiceDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<DocumentEWayInvoiceSummaryDto> GetDocumentSummaryAsync(Guid documentId, CancellationToken cancellationToken = default);
}
