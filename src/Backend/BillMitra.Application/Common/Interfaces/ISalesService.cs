using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Sales;

namespace BillMitra.Application.Common.Interfaces;

public interface ISalesService
{
    CalculateGstResponse CalculateGst(CalculateGstRequest request);
    Task<SalesDocumentDto> CreateDraftAsync(CreateOrUpdateSalesDocumentRequest request, string? user = null, CancellationToken ct = default);
    Task<SalesDocumentDto?> UpdateDraftAsync(Guid id, CreateOrUpdateSalesDocumentRequest request, string? user = null, CancellationToken ct = default);
    Task<SalesDocumentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SalesDocumentDto>> GetDocumentsAsync(DocumentType? type = null, DocumentStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? partyId = null, string? search = null, CancellationToken ct = default);
    Task<SalesDocumentDto> FinalizeDocumentAsync(Guid id, FinalizeDocumentRequest request, string? user = null, CancellationToken ct = default);
    Task<SalesDocumentDto> CancelDocumentAsync(Guid id, CancelDocumentRequest request, string? user = null, CancellationToken ct = default);
    Task<byte[]> GeneratePdfAsync(Guid id, string format = "a4", CancellationToken ct = default);
}

public interface IPdfGeneratorService
{
    byte[] GenerateDocumentPdf(SalesDocumentDto document, BillMitra.Contracts.Company.CompanyProfileDto company, string format);
}

