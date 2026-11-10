using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Company;
using BillMitra.Contracts.Sales;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class SalesService : ISalesService
{
    private readonly ISalesDocumentRepository _salesDocRepo;
    private readonly IDocumentSequenceRepository _sequenceRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IFinancialYearRepository _financialYearRepo;
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        ISalesDocumentRepository salesDocRepo,
        IDocumentSequenceRepository sequenceRepo,
        ICompanyRepository companyRepo,
        IFinancialYearRepository financialYearRepo,
        IPdfGeneratorService pdfGenerator,
        ILogger<SalesService> logger)
    {
        _salesDocRepo = salesDocRepo;
        _sequenceRepo = sequenceRepo;
        _companyRepo = companyRepo;
        _financialYearRepo = financialYearRepo;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    public CalculateGstResponse CalculateGst(CalculateGstRequest request)
    {
        var supplierState = (request.SupplierStateCode ?? string.Empty).Trim();
        var posState = (request.PlaceOfSupplyStateCode ?? string.Empty).Trim();

        var isIntraState = string.Equals(supplierState, posState, StringComparison.OrdinalIgnoreCase);
        var supplyType = isIntraState ? GstSupplyType.IntraState : GstSupplyType.InterState;

        var calculatedItems = new List<CalculatedItemResponse>();
        decimal subTotalTaxable = 0;
        decimal cgstTotal = 0;
        decimal sgstTotal = 0;
        decimal igstTotal = 0;
        decimal cessTotal = 0;

        for (int i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var quantity = Math.Max(0, item.Quantity);
            var unitPrice = Math.Max(0, item.UnitPrice);
            var discountPct = Math.Clamp(item.DiscountPercent, 0, 100);
            var gstRate = Math.Max(0, item.GstRatePercent);
            var cessRate = Math.Max(0, item.CessRatePercent);

            var gross = quantity * unitPrice;
            var discountAmt = Math.Round(gross * (discountPct / 100m), 2, MidpointRounding.AwayFromZero);
            var taxable = Math.Round(gross - discountAmt, 2, MidpointRounding.AwayFromZero);

            decimal cgstAmt = 0;
            decimal sgstAmt = 0;
            decimal igstAmt = 0;

            if (isIntraState)
            {
                var halfRate = gstRate / 2m;
                cgstAmt = Math.Round(taxable * (halfRate / 100m), 2, MidpointRounding.AwayFromZero);
                sgstAmt = Math.Round(taxable * (halfRate / 100m), 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                igstAmt = Math.Round(taxable * (gstRate / 100m), 2, MidpointRounding.AwayFromZero);
            }

            var cessAmt = Math.Round(taxable * (cessRate / 100m), 2, MidpointRounding.AwayFromZero);
            var lineTotal = taxable + cgstAmt + sgstAmt + igstAmt + cessAmt;

            subTotalTaxable += taxable;
            cgstTotal += cgstAmt;
            sgstTotal += sgstAmt;
            igstTotal += igstAmt;
            cessTotal += cessAmt;

            calculatedItems.Add(new CalculatedItemResponse(
                LineIndex: i + 1,
                ItemName: item.ItemName,
                HsnSacCode: item.HsnSacCode,
                Quantity: quantity,
                UnitPrice: unitPrice,
                DiscountPercent: discountPct,
                DiscountAmount: discountAmt,
                TaxableAmount: taxable,
                GstRatePercent: gstRate,
                CgstAmount: cgstAmt,
                SgstAmount: sgstAmt,
                IgstAmount: igstAmt,
                CessAmount: cessAmt,
                TotalAmount: lineTotal
            ));
        }

        var totalTax = cgstTotal + sgstTotal + igstTotal + cessTotal;
        var rawGrandTotal = subTotalTaxable + totalTax;
        var roundedGrandTotal = Math.Round(rawGrandTotal, 0, MidpointRounding.AwayFromZero);
        var roundOff = roundedGrandTotal - rawGrandTotal;
        var amountInWords = IndianCurrencyHelper.ConvertToWords(roundedGrandTotal);

        return new CalculateGstResponse(
            SupplyType: supplyType,
            SubTotalTaxable: subTotalTaxable,
            CgstTotal: cgstTotal,
            SgstTotal: sgstTotal,
            IgstTotal: igstTotal,
            CessTotal: cessTotal,
            TotalTax: totalTax,
            RoundOffAmount: roundOff,
            GrandTotal: roundedGrandTotal,
            TotalAmountInWords: amountInWords,
            Items: calculatedItems
        );
    }

    public async Task<SalesDocumentDto> CreateDraftAsync(CreateOrUpdateSalesDocumentRequest request, string? user = null, CancellationToken ct = default)
    {
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile is not initialized. Complete setup first.");

        var supplierStateCode = company.StateCode;
        var calcReq = new CalculateGstRequest(
            SupplierStateCode: supplierStateCode,
            PlaceOfSupplyStateCode: request.PlaceOfSupplyStateCode,
            IsReverseCharge: request.IsReverseCharge,
            Items: request.Items
        );

        var calcRes = CalculateGst(calcReq);

        var doc = new SalesDocument
        {
            Id = Guid.NewGuid(),
            DocumentType = (BillMitra.Domain.Enums.DocumentType)(int)request.DocumentType,
            DocumentStatus = BillMitra.Domain.Enums.DocumentStatus.Draft,
            DraftNumber = $"DFT-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}",
            DocumentDate = request.DocumentDate.Date,
            DueDate = request.DueDate?.Date,
            CompanyId = company.Id,
            PartyId = request.PartyId,
            CustomerName = request.CustomerName.Trim(),
            CustomerGstin = string.IsNullOrWhiteSpace(request.CustomerGstin) ? null : request.CustomerGstin.Trim().ToUpperInvariant(),
            BillingAddress = request.BillingAddress?.Trim(),
            ShippingAddress = request.ShippingAddress?.Trim(),
            PlaceOfSupplyStateCode = request.PlaceOfSupplyStateCode.Trim(),
            SupplierStateCode = supplierStateCode,
            IsReverseCharge = request.IsReverseCharge,
            SupplyType = (BillMitra.Domain.Enums.SupplyType)(int)calcRes.SupplyType,
            SubTotalTaxable = calcRes.SubTotalTaxable,
            CgstTotal = calcRes.CgstTotal,
            SgstTotal = calcRes.SgstTotal,
            IgstTotal = calcRes.IgstTotal,
            CessTotal = calcRes.CessTotal,
            TotalTax = calcRes.TotalTax,
            RoundOffAmount = calcRes.RoundOffAmount,
            GrandTotal = calcRes.GrandTotal,
            TotalAmountInWords = calcRes.TotalAmountInWords,
            Notes = request.Notes?.Trim(),
            TermsAndConditions = request.TermsAndConditions?.Trim(),
            CreatedBy = user,
            CreatedAtUtc = DateTime.UtcNow
        };

        for (int i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var calcItem = calcRes.Items[i];
            var isIntra = calcRes.SupplyType == GstSupplyType.IntraState;

            doc.Items.Add(new SalesDocumentItem
            {
                Id = Guid.NewGuid(),
                SalesDocumentId = doc.Id,
                ProductId = item.ProductId,
                LineNumber = i + 1,
                ItemName = item.ItemName.Trim(),
                HsnSacCode = item.HsnSacCode.Trim(),
                Quantity = calcItem.Quantity,
                UnitCode = string.IsNullOrWhiteSpace(item.UnitCode) ? "PCS" : item.UnitCode.Trim().ToUpperInvariant(),
                UnitPrice = calcItem.UnitPrice,
                DiscountPercent = calcItem.DiscountPercent,
                DiscountAmount = calcItem.DiscountAmount,
                TaxableAmount = calcItem.TaxableAmount,
                GstRatePercent = calcItem.GstRatePercent,
                CgstRatePercent = isIntra ? calcItem.GstRatePercent / 2m : 0m,
                CgstAmount = calcItem.CgstAmount,
                SgstRatePercent = isIntra ? calcItem.GstRatePercent / 2m : 0m,
                SgstAmount = calcItem.SgstAmount,
                IgstRatePercent = !isIntra ? calcItem.GstRatePercent : 0m,
                IgstAmount = calcItem.IgstAmount,
                CessRatePercent = item.CessRatePercent,
                CessAmount = calcItem.CessAmount,
                TotalAmount = calcItem.TotalAmount
            });
        }

        var saved = await _salesDocRepo.InsertAsync(doc, ct);
        _logger.LogInformation("Saved sales document draft {DraftNumber} (ID: {Id})", saved.DraftNumber, saved.Id);
        return MapToDto(saved);
    }

    public async Task<SalesDocumentDto?> UpdateDraftAsync(Guid id, CreateOrUpdateSalesDocumentRequest request, string? user = null, CancellationToken ct = default)
    {
        var doc = await _salesDocRepo.GetByIdAsync(id, ct);
        if (doc == null) return null;

        if (doc.DocumentStatus != BillMitra.Domain.Enums.DocumentStatus.Draft)
        {
            throw new InvalidOperationException("Only draft documents can be modified. Finalized or cancelled documents are immutable.");
        }

        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile is not initialized.");

        var calcReq = new CalculateGstRequest(
            SupplierStateCode: company.StateCode,
            PlaceOfSupplyStateCode: request.PlaceOfSupplyStateCode,
            IsReverseCharge: request.IsReverseCharge,
            Items: request.Items
        );

        var calcRes = CalculateGst(calcReq);

        doc.DocumentType = (BillMitra.Domain.Enums.DocumentType)(int)request.DocumentType;
        doc.DocumentDate = request.DocumentDate.Date;
        doc.DueDate = request.DueDate?.Date;
        doc.PartyId = request.PartyId;
        doc.CustomerName = request.CustomerName.Trim();
        doc.CustomerGstin = string.IsNullOrWhiteSpace(request.CustomerGstin) ? null : request.CustomerGstin.Trim().ToUpperInvariant();
        doc.BillingAddress = request.BillingAddress?.Trim();
        doc.ShippingAddress = request.ShippingAddress?.Trim();
        doc.PlaceOfSupplyStateCode = request.PlaceOfSupplyStateCode.Trim();
        doc.SupplierStateCode = company.StateCode;
        doc.IsReverseCharge = request.IsReverseCharge;
        doc.SupplyType = (BillMitra.Domain.Enums.SupplyType)(int)calcRes.SupplyType;
        doc.SubTotalTaxable = calcRes.SubTotalTaxable;
        doc.CgstTotal = calcRes.CgstTotal;
        doc.SgstTotal = calcRes.SgstTotal;
        doc.IgstTotal = calcRes.IgstTotal;
        doc.CessTotal = calcRes.CessTotal;
        doc.TotalTax = calcRes.TotalTax;
        doc.RoundOffAmount = calcRes.RoundOffAmount;
        doc.GrandTotal = calcRes.GrandTotal;
        doc.TotalAmountInWords = calcRes.TotalAmountInWords;
        doc.Notes = request.Notes?.Trim();
        doc.TermsAndConditions = request.TermsAndConditions?.Trim();

        doc.Items.Clear();
        for (int i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var calcItem = calcRes.Items[i];
            var isIntra = calcRes.SupplyType == GstSupplyType.IntraState;

            doc.Items.Add(new SalesDocumentItem
            {
                Id = Guid.NewGuid(),
                SalesDocumentId = doc.Id,
                ProductId = item.ProductId,
                LineNumber = i + 1,
                ItemName = item.ItemName.Trim(),
                HsnSacCode = item.HsnSacCode.Trim(),
                Quantity = calcItem.Quantity,
                UnitCode = string.IsNullOrWhiteSpace(item.UnitCode) ? "PCS" : item.UnitCode.Trim().ToUpperInvariant(),
                UnitPrice = calcItem.UnitPrice,
                DiscountPercent = calcItem.DiscountPercent,
                DiscountAmount = calcItem.DiscountAmount,
                TaxableAmount = calcItem.TaxableAmount,
                GstRatePercent = calcItem.GstRatePercent,
                CgstRatePercent = isIntra ? calcItem.GstRatePercent / 2m : 0m,
                CgstAmount = calcItem.CgstAmount,
                SgstRatePercent = isIntra ? calcItem.GstRatePercent / 2m : 0m,
                SgstAmount = calcItem.SgstAmount,
                IgstRatePercent = !isIntra ? calcItem.GstRatePercent : 0m,
                IgstAmount = calcItem.IgstAmount,
                CessRatePercent = item.CessRatePercent,
                CessAmount = calcItem.CessAmount,
                TotalAmount = calcItem.TotalAmount
            });
        }

        var updated = await _salesDocRepo.UpdateAsync(doc, ct);
        return MapToDto(updated);
    }

    public async Task<SalesDocumentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _salesDocRepo.GetByIdAsync(id, ct);
        return doc == null ? null : MapToDto(doc);
    }

    public async Task<IReadOnlyList<SalesDocumentDto>> GetDocumentsAsync(
        BillMitra.Contracts.Sales.DocumentType? type = null,
        BillMitra.Contracts.Sales.DocumentStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? partyId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var docs = await _salesDocRepo.GetAllAsync(
            (int?)type,
            (int?)status,
            fromDate,
            toDate,
            partyId,
            search,
            ct);

        return docs.Select(MapToDto).ToList();
    }

    public async Task<SalesDocumentDto> FinalizeDocumentAsync(Guid id, FinalizeDocumentRequest request, string? user = null, CancellationToken ct = default)
    {
        // Check idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _salesDocRepo.GetByIdempotencyKeyAsync(request.IdempotencyKey.Trim(), ct);
            if (existing != null)
            {
                _logger.LogInformation("Idempotent hit for key {Key}, returning existing invoice {Number}", request.IdempotencyKey, existing.DocumentNumber);
                return MapToDto(existing);
            }
        }

        var doc = await _salesDocRepo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Document with ID {id} was not found.");

        if (!doc.CanFinalize())
        {
            throw new InvalidOperationException($"Cannot finalize document in '{doc.DocumentStatus}' status. Only drafts can be finalized.");
        }

        var activeFy = await _financialYearRepo.GetActiveFinancialYearAsync(ct)
            ?? throw new InvalidOperationException("No active financial year found for document sequence numbering.");

        // Allocate sequence number server-side
        var allocatedNumber = await _sequenceRepo.AllocateNextNumberAsync(activeFy.Id, doc.DocumentType, ct);

        doc.DocumentNumber = allocatedNumber;
        doc.DocumentStatus = BillMitra.Domain.Enums.DocumentStatus.Finalized;
        doc.FinalizedAtUtc = DateTime.UtcNow;
        doc.IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();

        var updated = await _salesDocRepo.UpdateAsync(doc, ct);
        _logger.LogInformation("Finalized document {Id} with statutory number {Number}", updated.Id, updated.DocumentNumber);

        return MapToDto(updated);
    }

    public async Task<SalesDocumentDto> CancelDocumentAsync(Guid id, CancelDocumentRequest request, string? user = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CancellationReason))
        {
            throw new ArgumentException("A valid cancellation reason must be provided.", nameof(request.CancellationReason));
        }

        var doc = await _salesDocRepo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Document with ID {id} was not found.");

        if (!doc.CanCancel())
        {
            throw new InvalidOperationException($"Cannot cancel document in '{doc.DocumentStatus}' status. Only finalized documents can be cancelled.");
        }

        doc.DocumentStatus = BillMitra.Domain.Enums.DocumentStatus.Cancelled;
        doc.CancelledAtUtc = DateTime.UtcNow;
        doc.CancellationReason = request.CancellationReason.Trim();

        var updated = await _salesDocRepo.UpdateAsync(doc, ct);
        _logger.LogWarning("Cancelled document {Number} (ID: {Id}). Reason: {Reason}", updated.DocumentNumber, updated.Id, updated.CancellationReason);

        return MapToDto(updated);
    }

    public async Task<byte[]> GeneratePdfAsync(Guid id, string format = "a4", CancellationToken ct = default)
    {
        var doc = await GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Document with ID {id} was not found.");

        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var companyDto = new CompanyProfileDto
        {
            Id = company.Id,
            LegalName = company.LegalName,
            TradeName = company.TradeName,
            Gstin = company.Gstin,
            Pan = company.Pan,
            StateCode = company.StateCode,
            StateName = company.StateName,
            GstRegistrationType = company.GstRegistrationType,
            AddressLine1 = company.AddressLine1,
            AddressLine2 = company.AddressLine2,
            City = company.City,
            Pincode = company.Pincode,
            Email = company.Email,
            Phone = company.Phone,
            BankName = company.BankName,
            BankAccountNumber = company.BankAccountNumber,
            BankIfsc = company.BankIfsc,
            BankBranch = company.BankBranch,
            UpiId = company.UpiId,
            CreatedAtUtc = company.CreatedAtUtc,
            UpdatedAtUtc = company.UpdatedAtUtc
        };

        return _pdfGenerator.GenerateDocumentPdf(doc, companyDto, format);
    }

    private static SalesDocumentDto MapToDto(SalesDocument doc)
    {
        return new SalesDocumentDto(
            Id: doc.Id,
            DocumentType: (BillMitra.Contracts.Sales.DocumentType)(int)doc.DocumentType,
            DocumentStatus: (BillMitra.Contracts.Sales.DocumentStatus)(int)doc.DocumentStatus,
            DocumentNumber: doc.DocumentNumber,
            DraftNumber: doc.DraftNumber,
            DocumentDate: doc.DocumentDate,
            DueDate: doc.DueDate,
            CompanyId: doc.CompanyId,
            PartyId: doc.PartyId,
            CustomerName: doc.CustomerName,
            CustomerGstin: doc.CustomerGstin,
            BillingAddress: doc.BillingAddress,
            ShippingAddress: doc.ShippingAddress,
            PlaceOfSupplyStateCode: doc.PlaceOfSupplyStateCode,
            SupplierStateCode: doc.SupplierStateCode,
            IsReverseCharge: doc.IsReverseCharge,
            SupplyType: (BillMitra.Contracts.Sales.GstSupplyType)(int)doc.SupplyType,
            SubTotalTaxable: doc.SubTotalTaxable,
            CgstTotal: doc.CgstTotal,
            SgstTotal: doc.SgstTotal,
            IgstTotal: doc.IgstTotal,
            CessTotal: doc.CessTotal,
            TotalTax: doc.TotalTax,
            RoundOffAmount: doc.RoundOffAmount,
            GrandTotal: doc.GrandTotal,
            TotalAmountInWords: doc.TotalAmountInWords,
            Notes: doc.Notes,
            TermsAndConditions: doc.TermsAndConditions,
            CreatedAtUtc: doc.CreatedAtUtc,
            FinalizedAtUtc: doc.FinalizedAtUtc,
            CancelledAtUtc: doc.CancelledAtUtc,
            CancellationReason: doc.CancellationReason,
            Items: doc.Items.OrderBy(i => i.LineNumber).Select(i => new SalesDocumentItemDto(
                Id: i.Id,
                ProductId: i.ProductId,
                ItemName: i.ItemName,
                HsnSacCode: i.HsnSacCode,
                Quantity: i.Quantity,
                UnitCode: i.UnitCode,
                UnitPrice: i.UnitPrice,
                DiscountPercent: i.DiscountPercent,
                DiscountAmount: i.DiscountAmount,
                TaxableAmount: i.TaxableAmount,
                GstRatePercent: i.GstRatePercent,
                CgstRatePercent: i.CgstRatePercent,
                CgstAmount: i.CgstAmount,
                SgstRatePercent: i.SgstRatePercent,
                SgstAmount: i.SgstAmount,
                IgstRatePercent: i.IgstRatePercent,
                IgstAmount: i.IgstAmount,
                CessRatePercent: i.CessRatePercent,
                CessAmount: i.CessAmount,
                TotalAmount: i.TotalAmount
            )).ToList()
        );
    }
}
