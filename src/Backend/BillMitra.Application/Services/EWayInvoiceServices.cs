using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.EWayInvoice;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class EWayBillService : IEWayBillService
{
    private readonly IEWayBillRepository _ewayBillRepository;
    private readonly ISalesDocumentRepository _salesRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IEWayBillProvider _provider;
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly ILogger<EWayBillService> _logger;

    public EWayBillService(
        IEWayBillRepository ewayBillRepository,
        ISalesDocumentRepository salesRepository,
        ICompanyRepository companyRepository,
        IEWayBillProvider provider,
        IPdfGeneratorService pdfGenerator,
        ILogger<EWayBillService> logger)
    {
        _ewayBillRepository = ewayBillRepository;
        _salesRepository = salesRepository;
        _companyRepository = companyRepository;
        _provider = provider;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    public async Task<EWayBillDto> GenerateAsync(GenerateEWayBillRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await _salesRepository.GetByIdAsync(request.SalesDocumentId, cancellationToken);
        if (doc == null)
            throw new InvalidOperationException($"Sales document {request.SalesDocumentId} not found.");

        if (doc.DocumentStatus == DocumentStatus.Draft)
            throw new InvalidOperationException("Cannot generate E-Way Bill for draft documents. Please finalize first.");

        if (doc.DocumentStatus == DocumentStatus.Cancelled)
            throw new InvalidOperationException("Cannot generate E-Way Bill for a cancelled document.");

        var existing = await _ewayBillRepository.GetByDocumentIdAsync(request.SalesDocumentId, cancellationToken);
        if (existing != null && existing.Status != EWayBillStatus.Cancelled)
            throw new InvalidOperationException($"An active E-Way Bill ({existing.EWayBillNumber}) already exists for this document.");

        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (company == null)
            throw new InvalidOperationException("Company profile not configured.");

        var record = new EWayBillRecord
        {
            SalesDocumentId = doc.Id,
            DocType = doc.DocumentType == DocumentType.TaxInvoice ? "INV" : "CHL",
            DocNumber = doc.DocumentNumber ?? doc.DraftNumber,
            DocDate = doc.DocumentDate,
            SupplyType = doc.SupplyType,
            SubSupplyType = EWayBillSubSupplyType.Supply,
            FromGstin = company.Gstin ?? "URP",
            FromName = company.LegalName,
            FromAddress = $"{company.AddressLine1} {company.AddressLine2}".Trim(),
            FromPlace = company.City,
            FromPincode = company.Pincode,
            FromStateCode = company.StateCode,
            ToGstin = !string.IsNullOrWhiteSpace(doc.CustomerGstin) ? doc.CustomerGstin : "URP",
            ToName = doc.CustomerName,
            ToAddress = doc.BillingAddress ?? "Address",
            ToPlace = "City",
            ToPincode = "000000",
            ToStateCode = doc.PlaceOfSupplyStateCode,
            TotalInvoiceValue = doc.GrandTotal,
            TaxableAmount = doc.SubTotalTaxable,
            CgstAmount = doc.CgstTotal,
            SgstAmount = doc.SgstTotal,
            IgstAmount = doc.IgstTotal,
            CessAmount = doc.CessTotal,
            TransportationMode = (TransportationMode)request.TransportationMode,
            TransporterId = request.TransporterId,
            TransporterName = request.TransporterName,
            TransportDocNo = request.TransportDocNo,
            TransportDocDate = request.TransportDocDate,
            VehicleNo = request.VehicleNo,
            VehicleType = (VehicleType)request.VehicleType,
            ApproximateDistanceKm = request.ApproximateDistanceKm > 0 ? request.ApproximateDistanceKm : 10
        };

        var result = await _provider.GenerateEWayBillAsync(record, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.EWayBillNumber))
            throw new InvalidOperationException(result.ErrorMessage ?? "E-Way Bill generation failed at provider.");

        record.EWayBillNumber = result.EWayBillNumber;
        record.EWayBillDate = result.EWayBillDate;
        record.ValidUntil = result.ValidUntil;
        record.Status = EWayBillStatus.Generated;

        await _ewayBillRepository.InsertAsync(record, cancellationToken);
        _logger.LogInformation("Generated E-Way Bill {EwbNo} for document {DocNo}", record.EWayBillNumber, doc.DocumentNumber);

        return ToDto(record);
    }

    public async Task<EWayBillDto> UpdateVehicleAsync(UpdatePartBRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _ewayBillRepository.GetByEWayBillNumberAsync(request.EWayBillNumber, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"E-Way Bill {request.EWayBillNumber} not found.");

        if (record.Status == EWayBillStatus.Cancelled)
            throw new InvalidOperationException("Cannot update vehicle for a cancelled E-Way Bill.");

        await _provider.UpdateVehicleAsync(request.EWayBillNumber, request.VehicleNo, request.FromPlace, request.FromStateCode, request.ReasonCode, request.Remarks, cancellationToken);

        record.VehicleNo = request.VehicleNo;
        record.Status = EWayBillStatus.Updated;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await _ewayBillRepository.UpdateAsync(record, cancellationToken);
        return ToDto(record);
    }

    public async Task<EWayBillDto> CancelAsync(CancelEWayBillRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _ewayBillRepository.GetByEWayBillNumberAsync(request.EWayBillNumber, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"E-Way Bill {request.EWayBillNumber} not found.");

        if (!record.CanCancel())
            throw new InvalidOperationException("E-Way Bill cancellation is only permitted within 24 hours of generation.");

        await _provider.CancelEWayBillAsync(request.EWayBillNumber, request.CancelReasonCode, request.CancelRemarks, cancellationToken);

        record.Status = EWayBillStatus.Cancelled;
        record.CancelReason = request.CancelReasonCode;
        record.CancelRemarks = request.CancelRemarks;
        record.CancelledAt = DateTimeOffset.UtcNow;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await _ewayBillRepository.UpdateAsync(record, cancellationToken);
        return ToDto(record);
    }

    public async Task<EWayBillDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _ewayBillRepository.GetByIdAsync(id, cancellationToken);
        return record == null ? null : ToDto(record);
    }

    public async Task<EWayBillDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var record = await _ewayBillRepository.GetByDocumentIdAsync(documentId, cancellationToken);
        return record == null ? null : ToDto(record);
    }

    public async Task<IReadOnlyList<EWayBillDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var records = await _ewayBillRepository.ListAsync(page, pageSize, cancellationToken);
        return records.Select(ToDto).ToList();
    }

    public async Task<byte[]> GeneratePdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ewb = await GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"E-Way Bill with ID {id} was not found.");

        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var companyDto = new BillMitra.Contracts.Company.CompanyProfileDto
        {
            Id = company.Id,
            LegalName = company.LegalName,
            TradeName = company.TradeName,
            Gstin = company.Gstin,
            Pan = company.Pan,
            StateCode = company.StateCode,
            StateName = company.StateName,
            AddressLine1 = company.AddressLine1,
            City = company.City,
            Pincode = company.Pincode,
            Phone = company.Phone,
            Email = company.Email
        };

        return _pdfGenerator.GenerateEWayBillPdf(ewb, companyDto);
    }

    private static EWayBillDto ToDto(EWayBillRecord r) => new(
        r.Id,
        r.SalesDocumentId,
        r.EWayBillNumber,
        r.EWayBillDate,
        r.ValidUntil,
        (int)r.Status,
        r.Status.ToString(),
        r.DocType,
        r.DocNumber,
        r.DocDate,
        r.FromGstin,
        r.FromName,
        r.ToGstin ?? string.Empty,
        r.ToName,
        r.TotalInvoiceValue,
        (int)r.TransportationMode,
        r.TransporterId,
        r.TransporterName,
        r.VehicleNo,
        r.ApproximateDistanceKm,
        r.CanCancel(),
        r.CancelReason,
        r.CancelRemarks,
        r.CancelledAt
    );
}

public class EInvoiceService : IEInvoiceService
{
    private readonly IEInvoiceRepository _einvoiceRepository;
    private readonly ISalesDocumentRepository _salesRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IEWayBillRepository _ewayBillRepository;
    private readonly IEInvoiceProvider _provider;
    private readonly ILogger<EInvoiceService> _logger;

    public EInvoiceService(
        IEInvoiceRepository einvoiceRepository,
        ISalesDocumentRepository salesRepository,
        ICompanyRepository companyRepository,
        IEWayBillRepository ewayBillRepository,
        IEInvoiceProvider provider,
        ILogger<EInvoiceService> logger)
    {
        _einvoiceRepository = einvoiceRepository;
        _salesRepository = salesRepository;
        _companyRepository = companyRepository;
        _ewayBillRepository = ewayBillRepository;
        _provider = provider;
        _logger = logger;
    }

    public async Task<EInvoiceDto> GenerateAsync(GenerateEInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await _salesRepository.GetByIdAsync(request.SalesDocumentId, cancellationToken);
        if (doc == null)
            throw new InvalidOperationException($"Sales document {request.SalesDocumentId} not found.");

        if (doc.DocumentStatus == DocumentStatus.Draft)
            throw new InvalidOperationException("Cannot generate E-Invoice (IRN) for draft documents. Please finalize first.");

        if (doc.DocumentStatus == DocumentStatus.Cancelled)
            throw new InvalidOperationException("Cannot generate E-Invoice for a cancelled document.");

        if (string.IsNullOrWhiteSpace(doc.CustomerGstin))
            throw new InvalidOperationException("E-Invoicing (IRN) requires a registered B2B customer with a valid GSTIN.");

        var existing = await _einvoiceRepository.GetByDocumentIdAsync(request.SalesDocumentId, cancellationToken);
        if (existing != null && existing.Status != EInvoiceStatus.Cancelled)
            throw new InvalidOperationException($"An active IRN already exists for document {doc.DocumentNumber}.");

        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (company == null || string.IsNullOrWhiteSpace(company.Gstin))
            throw new InvalidOperationException("Company profile with valid GSTIN is required for E-Invoicing.");

        var context = new EInvoiceGenerationContext(
            doc.Id,
            doc.DocumentNumber ?? doc.DraftNumber,
            doc.DocumentDate,
            doc.DocumentType.ToString(),
            company.Gstin,
            company.LegalName,
            doc.CustomerGstin,
            doc.CustomerName,
            doc.PlaceOfSupplyStateCode,
            doc.SubTotalTaxable,
            doc.CgstTotal,
            doc.SgstTotal,
            doc.IgstTotal,
            doc.CessTotal,
            doc.GrandTotal
        );

        var result = await _provider.GenerateIrnAsync(context, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Irn))
            throw new InvalidOperationException(result.ErrorMessage ?? "IRN generation failed at IRP provider.");

        var record = new EInvoiceRecord
        {
            SalesDocumentId = doc.Id,
            Irn = result.Irn,
            AckNo = result.AckNo ?? string.Empty,
            AckDate = result.AckDate,
            SignedInvoice = result.SignedInvoice ?? string.Empty,
            SignedQrCode = result.SignedQrCode ?? string.Empty,
            Status = EInvoiceStatus.Generated
        };

        await _einvoiceRepository.InsertAsync(record, cancellationToken);
        _logger.LogInformation("Generated IRN {Irn} for document {DocNo}", record.Irn, doc.DocumentNumber);

        return ToDto(record);
    }

    public async Task<EInvoiceDto> CancelAsync(CancelEInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _einvoiceRepository.GetByIrnAsync(request.Irn, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"E-Invoice with IRN {request.Irn} not found.");

        if (!record.CanCancel())
            throw new InvalidOperationException("IRN cancellation is only permitted within 24 hours of generation.");

        await _provider.CancelIrnAsync(request.Irn, request.CancelReason, request.CancelRemarks, cancellationToken);

        record.Status = EInvoiceStatus.Cancelled;
        record.CancelReason = (EInvoiceCancelReason)request.CancelReason;
        record.CancelRemarks = request.CancelRemarks;
        record.CancelledAt = DateTimeOffset.UtcNow;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await _einvoiceRepository.UpdateAsync(record, cancellationToken);
        return ToDto(record);
    }

    public async Task<EInvoiceDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var record = await _einvoiceRepository.GetByDocumentIdAsync(documentId, cancellationToken);
        return record == null ? null : ToDto(record);
    }

    public async Task<IReadOnlyList<EInvoiceDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var records = await _einvoiceRepository.ListAsync(page, pageSize, cancellationToken);
        return records.Select(ToDto).ToList();
    }

    public async Task<DocumentEWayInvoiceSummaryDto> GetDocumentSummaryAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var doc = await _salesRepository.GetByIdAsync(documentId, cancellationToken);
        if (doc == null)
            throw new InvalidOperationException($"Document {documentId} not found.");

        var eway = await _ewayBillRepository.GetByDocumentIdAsync(documentId, cancellationToken);
        var einv = await _einvoiceRepository.GetByDocumentIdAsync(documentId, cancellationToken);

        var isEWayBillMandatory = doc.GrandTotal > 50000m;
        var isEInvoiceEligible = !string.IsNullOrWhiteSpace(doc.CustomerGstin) &&
                               (doc.DocumentType == DocumentType.TaxInvoice ||
                                doc.DocumentType == DocumentType.CreditNote ||
                                doc.DocumentType == DocumentType.DebitNote);

        return new DocumentEWayInvoiceSummaryDto(
            doc.Id,
            doc.DocumentNumber ?? doc.DraftNumber,
            doc.GrandTotal,
            isEWayBillMandatory,
            isEInvoiceEligible,
            eway != null ? new EWayBillDto(
                eway.Id, eway.SalesDocumentId, eway.EWayBillNumber, eway.EWayBillDate, eway.ValidUntil,
                (int)eway.Status, eway.Status.ToString(), eway.DocType, eway.DocNumber, eway.DocDate,
                eway.FromGstin, eway.FromName, eway.ToGstin ?? string.Empty, eway.ToName, eway.TotalInvoiceValue,
                (int)eway.TransportationMode, eway.TransporterId, eway.TransporterName, eway.VehicleNo,
                eway.ApproximateDistanceKm, eway.CanCancel(), eway.CancelReason, eway.CancelRemarks, eway.CancelledAt
            ) : null,
            einv != null ? new EInvoiceDto(
                einv.Id, einv.SalesDocumentId, einv.Irn, einv.AckNo, einv.AckDate, einv.SignedQrCode,
                (int)einv.Status, einv.Status.ToString(), einv.CanCancel(), (int?)einv.CancelReason,
                einv.CancelRemarks, einv.CancelledAt
            ) : null
        );
    }

    private static EInvoiceDto ToDto(EInvoiceRecord r) => new(
        r.Id,
        r.SalesDocumentId,
        r.Irn,
        r.AckNo,
        r.AckDate,
        r.SignedQrCode,
        (int)r.Status,
        r.Status.ToString(),
        r.CanCancel(),
        (int?)r.CancelReason,
        r.CancelRemarks,
        r.CancelledAt
    );
}
