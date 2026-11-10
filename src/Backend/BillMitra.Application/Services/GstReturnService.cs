using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Gst;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class GstReturnService : IGstReturnService
{
    private readonly IGstReturnRepository _gstRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IFinancialYearRepository _fyRepo;
    private readonly IGstReturnProvider _provider;
    private readonly ILogger<GstReturnService> _logger;

    public GstReturnService(
        IGstReturnRepository gstRepo,
        ICompanyRepository companyRepo,
        IFinancialYearRepository fyRepo,
        IGstReturnProvider provider,
        ILogger<GstReturnService> logger)
    {
        _gstRepo = gstRepo;
        _companyRepo = companyRepo;
        _fyRepo = fyRepo;
        _provider = provider;
        _logger = logger;
    }

    public async Task<Gstr1SummaryDto> GetGstr1SummaryAsync(string taxPeriod, CancellationToken ct = default)
    {
        var (startDate, endDate, formattedPeriod) = ParseTaxPeriod(taxPeriod);
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var fy = await _fyRepo.GetActiveFinancialYearAsync(ct);
        var fyCode = fy?.Code ?? $"{startDate.Year}-{startDate.AddYears(1).ToString("yy")}";

        var allDocs = await _gstRepo.GetFinalizedDocumentsForPeriodAsync(company.Id, startDate, endDate, ct);
        var activeDocs = allDocs.Where(d => d.DocumentStatus != DocumentStatus.Cancelled).ToList();

        // 1. Table 4A: B2B Invoices
        var b2bDocs = activeDocs.Where(d =>
            (d.DocumentType == DocumentType.TaxInvoice || d.DocumentType == DocumentType.BillOfSupply) &&
            !string.IsNullOrWhiteSpace(d.CustomerGstin)).ToList();

        var b2bInvoices = b2bDocs.Select(d => new Gstr1B2bInvoiceDto(
            CustomerGstin: d.CustomerGstin!,
            CustomerName: d.CustomerName,
            InvoiceNumber: d.DocumentNumber ?? d.DraftNumber,
            InvoiceDate: d.DocumentDate,
            InvoiceValue: d.GrandTotal,
            PlaceOfSupply: d.PlaceOfSupplyStateCode,
            ReverseCharge: d.IsReverseCharge,
            InvoiceType: "Regular",
            TaxableValue: d.SubTotalTaxable,
            CgstAmount: d.CgstTotal,
            SgstAmount: d.SgstTotal,
            IgstAmount: d.IgstTotal,
            CessAmount: d.CessTotal,
            Items: GroupItemsByRate(d.Items)
        )).ToList();

        // 2. Table 5A: B2CL (Inter-state unregistered > 2.5 Lakhs)
        var b2clDocs = activeDocs.Where(d =>
            (d.DocumentType == DocumentType.TaxInvoice || d.DocumentType == DocumentType.BillOfSupply) &&
            string.IsNullOrWhiteSpace(d.CustomerGstin) &&
            (d.SupplyType == SupplyType.InterState || d.PlaceOfSupplyStateCode != company.StateCode) &&
            d.GrandTotal > 250000m).ToList();

        var b2clInvoices = b2clDocs.Select(d => new Gstr1B2clInvoiceDto(
            InvoiceNumber: d.DocumentNumber ?? d.DraftNumber,
            InvoiceDate: d.DocumentDate,
            InvoiceValue: d.GrandTotal,
            PlaceOfSupply: d.PlaceOfSupplyStateCode,
            TaxableValue: d.SubTotalTaxable,
            IgstAmount: d.IgstTotal,
            CessAmount: d.CessTotal,
            Items: GroupItemsByRate(d.Items)
        )).ToList();

        // 3. Table 7: B2CS (Small Consumer supplies)
        var b2csDocs = activeDocs.Where(d =>
            (d.DocumentType == DocumentType.TaxInvoice || d.DocumentType == DocumentType.BillOfSupply) &&
            string.IsNullOrWhiteSpace(d.CustomerGstin) &&
            ((d.SupplyType == SupplyType.IntraState && d.PlaceOfSupplyStateCode == company.StateCode) ||
             ((d.SupplyType == SupplyType.InterState || d.PlaceOfSupplyStateCode != company.StateCode) && d.GrandTotal <= 250000m))
        ).ToList();

        var b2csSections = b2csDocs
            .SelectMany(d => d.Items.Select(item => new
            {
                SupplyType = (d.SupplyType == SupplyType.IntraState && d.PlaceOfSupplyStateCode == company.StateCode) ? "INTRA" : "INTER",
                Pos = d.PlaceOfSupplyStateCode,
                Rate = item.GstRatePercent,
                Taxable = item.TaxableAmount,
                Cgst = item.CgstAmount,
                Sgst = item.SgstAmount,
                Igst = item.IgstAmount,
                Cess = item.CessAmount
            }))
            .GroupBy(x => new { x.SupplyType, x.Pos, x.Rate })
            .Select(g => new Gstr1B2csSectionDto(
                SupplyType: g.Key.SupplyType,
                PlaceOfSupply: g.Key.Pos,
                GstRatePercent: g.Key.Rate,
                TaxableValue: Math.Round(g.Sum(x => x.Taxable), 2),
                CgstAmount: Math.Round(g.Sum(x => x.Cgst), 2),
                SgstAmount: Math.Round(g.Sum(x => x.Sgst), 2),
                IgstAmount: Math.Round(g.Sum(x => x.Igst), 2),
                CessAmount: Math.Round(g.Sum(x => x.Cess), 2)
            )).ToList();

        // 4. Table 9B: CDNR (Credit/Debit Notes to Registered)
        var cdnrDocs = activeDocs.Where(d =>
            (d.DocumentType == DocumentType.CreditNote || d.DocumentType == DocumentType.DebitNote) &&
            !string.IsNullOrWhiteSpace(d.CustomerGstin)).ToList();

        var cdnrNotes = cdnrDocs.Select(d => new Gstr1CdnrNoteDto(
            CustomerGstin: d.CustomerGstin!,
            CustomerName: d.CustomerName,
            NoteNumber: d.DocumentNumber ?? d.DraftNumber,
            NoteDate: d.DocumentDate,
            NoteType: d.DocumentType == DocumentType.CreditNote ? "C" : "D",
            OriginalInvoiceNumber: d.SourceDocumentNumber ?? "PRE-GST",
            OriginalInvoiceDate: null,
            NoteValue: d.GrandTotal,
            TaxableValue: d.SubTotalTaxable,
            CgstAmount: d.CgstTotal,
            SgstAmount: d.SgstTotal,
            IgstAmount: d.IgstTotal,
            CessAmount: d.CessTotal
        )).ToList();

        // 5. Table 9B: CDNUR (Credit/Debit Notes to Unregistered)
        var cdnurDocs = activeDocs.Where(d =>
            (d.DocumentType == DocumentType.CreditNote || d.DocumentType == DocumentType.DebitNote) &&
            string.IsNullOrWhiteSpace(d.CustomerGstin)).ToList();

        // 6. Table 12: HSN Summary count
        var hsnList = await GetHsnSummaryAsync(taxPeriod, ct);

        // 7. Table 13: Document Summary
        var docSummaries = new List<Gstr1DocSummaryDto>();
        var docGroups = allDocs.GroupBy(d => d.DocumentType).OrderBy(g => g.Key);

        foreach (var g in docGroups)
        {
            var total = g.Count();
            var cancelled = g.Count(d => d.DocumentStatus == DocumentStatus.Cancelled);
            var sorted = g.OrderBy(d => d.DocumentNumber ?? d.DraftNumber).ToList();
            var from = sorted.First().DocumentNumber ?? sorted.First().DraftNumber;
            var to = sorted.Last().DocumentNumber ?? sorted.Last().DraftNumber;

            docSummaries.Add(new Gstr1DocSummaryDto(
                DocumentType: (int)g.Key,
                DocumentName: GetDocTypeName(g.Key),
                FromSerial: from,
                ToSerial: to,
                TotalCount: total,
                CancelledCount: cancelled,
                NetIssuedCount: total - cancelled
            ));
        }

        // Aggregate Totals
        var totalTaxable = activeDocs.Sum(d => d.SubTotalTaxable);
        var totalCgst = activeDocs.Sum(d => d.CgstTotal);
        var totalSgst = activeDocs.Sum(d => d.SgstTotal);
        var totalIgst = activeDocs.Sum(d => d.IgstTotal);
        var totalCess = activeDocs.Sum(d => d.CessTotal);
        var totalTax = totalCgst + totalSgst + totalIgst + totalCess;
        var grandTotal = activeDocs.Sum(d => d.GrandTotal);

        return new Gstr1SummaryDto(
            TaxPeriod: formattedPeriod,
            FinancialYear: fyCode,
            B2bTotalTaxable: b2bInvoices.Sum(x => x.TaxableValue),
            B2bTotalTax: b2bInvoices.Sum(x => x.CgstAmount + x.SgstAmount + x.IgstAmount + x.CessAmount),
            B2bCount: b2bInvoices.Count,
            B2clTotalTaxable: b2clInvoices.Sum(x => x.TaxableValue),
            B2clTotalTax: b2clInvoices.Sum(x => x.IgstAmount + x.CessAmount),
            B2clCount: b2clInvoices.Count,
            B2csTotalTaxable: b2csSections.Sum(x => x.TaxableValue),
            B2csTotalTax: b2csSections.Sum(x => x.CgstAmount + x.SgstAmount + x.IgstAmount + x.CessAmount),
            B2csCount: b2csDocs.Count,
            CdnrTotalTaxable: cdnrNotes.Sum(x => x.TaxableValue),
            CdnrTotalTax: cdnrNotes.Sum(x => x.CgstAmount + x.SgstAmount + x.IgstAmount + x.CessAmount),
            CdnrCount: cdnrNotes.Count,
            CdnurTotalTaxable: cdnurDocs.Sum(x => x.SubTotalTaxable),
            CdnurTotalTax: cdnurDocs.Sum(x => x.TotalTax),
            CdnurCount: cdnurDocs.Count,
            HsnCount: hsnList.Count,
            TotalInvoicesCount: allDocs.Count,
            TotalCancelledCount: allDocs.Count(d => d.DocumentStatus == DocumentStatus.Cancelled),
            TotalTaxableValue: Math.Round(totalTaxable, 2),
            TotalCgst: Math.Round(totalCgst, 2),
            TotalSgst: Math.Round(totalSgst, 2),
            TotalIgst: Math.Round(totalIgst, 2),
            TotalCess: Math.Round(totalCess, 2),
            TotalTaxLiability: Math.Round(totalTax, 2),
            GrandTotal: Math.Round(grandTotal, 2),
            B2bInvoices: b2bInvoices,
            B2clInvoices: b2clInvoices,
            B2csSections: b2csSections,
            CdnrNotes: cdnrNotes,
            DocumentsSummary: docSummaries
        );
    }

    public async Task<Gstr1JsonRoot> GenerateGstr1JsonPayloadAsync(string taxPeriod, CancellationToken ct = default)
    {
        var summary = await GetGstr1SummaryAsync(taxPeriod, ct);
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var root = new Gstr1JsonRoot
        {
            gstin = company.Gstin ?? "URP",
            fp = summary.TaxPeriod,
            cur_gt = summary.GrandTotal,
            gt = summary.GrandTotal
        };

        // B2B Grouping
        var b2bGroups = summary.B2bInvoices
            .GroupBy(inv => inv.CustomerGstin)
            .Select(g => new Gstr1JsonB2bGroup
            {
                ctin = g.Key,
                inv = g.Select(inv => new Gstr1JsonInvoice
                {
                    inum = inv.InvoiceNumber,
                    idt = inv.InvoiceDate.ToString("dd-MM-yyyy"),
                    val = inv.InvoiceValue,
                    pos = inv.PlaceOfSupply,
                    rchrg = inv.ReverseCharge ? "Y" : "N",
                    inv_typ = "R",
                    itms = inv.Items.Select((item, idx) => new Gstr1JsonInvoiceItem
                    {
                        num = idx + 1,
                        itm_det = new Gstr1JsonItemDetail
                        {
                            rt = item.RatePercent,
                            txval = item.TaxableValue,
                            iamt = item.IgstAmount,
                            camt = item.CgstAmount,
                            samt = item.SgstAmount,
                            csamt = item.CessAmount
                        }
                    }).ToList()
                }).ToList()
            }).ToList();
        root.b2b = b2bGroups;

        // B2CL Grouping
        var b2clGroups = summary.B2clInvoices
            .GroupBy(inv => inv.PlaceOfSupply)
            .Select(g => new Gstr1JsonB2clGroup
            {
                pos = g.Key,
                inv = g.Select(inv => new Gstr1JsonInvoice
                {
                    inum = inv.InvoiceNumber,
                    idt = inv.InvoiceDate.ToString("dd-MM-yyyy"),
                    val = inv.InvoiceValue,
                    pos = inv.PlaceOfSupply,
                    rchrg = "N",
                    inv_typ = "R",
                    itms = inv.Items.Select((item, idx) => new Gstr1JsonInvoiceItem
                    {
                        num = idx + 1,
                        itm_det = new Gstr1JsonItemDetail
                        {
                            rt = item.RatePercent,
                            txval = item.TaxableValue,
                            iamt = item.IgstAmount,
                            camt = 0,
                            samt = 0,
                            csamt = item.CessAmount
                        }
                    }).ToList()
                }).ToList()
            }).ToList();
        root.b2cl = b2clGroups;

        // B2CS Items
        root.b2cs = summary.B2csSections.Select(s => new Gstr1JsonB2csItem
        {
            sply_ty = s.SupplyType,
            pos = s.PlaceOfSupply,
            typ = "OE",
            rt = s.GstRatePercent,
            txval = s.TaxableValue,
            iamt = s.IgstAmount,
            camt = s.CgstAmount,
            samt = s.SgstAmount,
            csamt = s.CessAmount
        }).ToList();

        // CDNR Grouping
        var cdnrGroups = summary.CdnrNotes
            .GroupBy(n => n.CustomerGstin)
            .Select(g => new Gstr1JsonCdnrGroup
            {
                ctin = g.Key,
                nt = g.Select(n => new Gstr1JsonCdnrNote
                {
                    ntty = n.NoteType,
                    nt_num = n.NoteNumber,
                    nt_dt = n.NoteDate.ToString("dd-MM-yyyy"),
                    inum = n.OriginalInvoiceNumber,
                    idt = n.OriginalInvoiceDate?.ToString("dd-MM-yyyy") ?? n.NoteDate.ToString("dd-MM-yyyy"),
                    val = n.NoteValue,
                    pos = company.StateCode,
                    rchrg = "N",
                    itms = new List<Gstr1JsonInvoiceItem>
                    {
                        new()
                        {
                            num = 1,
                            itm_det = new Gstr1JsonItemDetail
                            {
                                rt = 18m,
                                txval = n.TaxableValue,
                                iamt = n.IgstAmount,
                                camt = n.CgstAmount,
                                samt = n.SgstAmount,
                                csamt = n.CessAmount
                            }
                        }
                    }
                }).ToList()
            }).ToList();
        root.cdnr = cdnrGroups;

        // Table 12: HSN
        var hsnData = await GetHsnSummaryAsync(taxPeriod, ct);
        root.hsn = new Gstr1JsonHsnTable
        {
            data = hsnData.Select((h, idx) => new Gstr1JsonHsnItem
            {
                num = idx + 1,
                hsn_sc = h.HsnSacCode,
                desc = h.Description,
                uqc = string.IsNullOrWhiteSpace(h.UnitCode) ? "PCS" : h.UnitCode,
                qty = h.TotalQuantity,
                val = h.TotalValue,
                txval = h.TaxableAmount,
                iamt = h.IgstAmount,
                camt = h.CgstAmount,
                samt = h.SgstAmount,
                csamt = h.CessAmount
            }).ToList()
        };

        // Table 13: Doc Issue
        root.doc_issue = new Gstr1JsonDocIssueTable
        {
            doc_det = summary.DocumentsSummary.Select(d => new Gstr1JsonDocGroup
            {
                doc_num = d.DocumentType,
                docs = new List<Gstr1JsonDocRow>
                {
                    new()
                    {
                        num = 1,
                        from = d.FromSerial,
                        to = d.ToSerial,
                        totnum = d.TotalCount,
                        canc = d.CancelledCount,
                        net_issue = d.NetIssuedCount
                    }
                }
            }).ToList()
        };

        return root;
    }

    public async Task<GstValidationResultDto> ValidateGstr1Async(string taxPeriod, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var company = await _companyRepo.GetPrimaryCompanyAsync(ct);
        if (company == null)
        {
            errors.Add("Company profile has not been initialized.");
            return new GstValidationResultDto(false, errors, warnings);
        }

        if (string.IsNullOrWhiteSpace(company.Gstin) || !CompanyProfile.IsValidGstin(company.Gstin))
        {
            errors.Add($"Company GSTIN '{company.Gstin}' is invalid or missing. A valid 15-character GSTIN is mandatory to file GSTR-1.");
        }

        var (startDate, endDate, _) = ParseTaxPeriod(taxPeriod);
        var docs = await _gstRepo.GetFinalizedDocumentsForPeriodAsync(company.Id, startDate, endDate, ct);
        var activeDocs = docs.Where(d => d.DocumentStatus != DocumentStatus.Cancelled).ToList();

        if (activeDocs.Count == 0)
        {
            warnings.Add($"No sales documents or invoices were found for the period {taxPeriod}. GSTR-1 will be prepared as a Nil Return.");
            return new GstValidationResultDto(errors.Count == 0, errors, warnings);
        }

        foreach (var doc in activeDocs)
        {
            var docRef = doc.DocumentNumber ?? doc.DraftNumber;

            // Check B2B GSTIN validity
            if (!string.IsNullOrWhiteSpace(doc.CustomerGstin))
            {
                if (!CompanyProfile.IsValidGstin(doc.CustomerGstin))
                {
                    errors.Add($"Invoice {docRef}: Customer GSTIN '{doc.CustomerGstin}' is invalid. B2B invoices require a valid 15-character alphanumeric GSTIN.");
                }
            }

            // Check Place of Supply
            if (string.IsNullOrWhiteSpace(doc.PlaceOfSupplyStateCode) || doc.PlaceOfSupplyStateCode.Length != 2)
            {
                errors.Add($"Invoice {docRef}: Missing or invalid Place of Supply state code ('{doc.PlaceOfSupplyStateCode}').");
            }

            // Check items and HSN codes
            if (doc.Items.Count == 0)
            {
                errors.Add($"Invoice {docRef} has no line items.");
            }

            foreach (var item in doc.Items)
            {
                if (string.IsNullOrWhiteSpace(item.HsnSacCode))
                {
                    warnings.Add($"Invoice {docRef}, Line #{item.LineNumber}: Product '{item.ItemName}' is missing an HSN/SAC code. CBIC requires 4-6 digit HSN codes.");
                }

                if (item.TaxableAmount < 0)
                {
                    errors.Add($"Invoice {docRef}, Line #{item.LineNumber}: Negative taxable value is not permitted.");
                }
            }
        }

        return new GstValidationResultDto(errors.Count == 0, errors, warnings);
    }

    public async Task<Gstr3bSummaryDto> GetGstr3bSummaryAsync(string taxPeriod, CancellationToken ct = default)
    {
        var (startDate, endDate, formattedPeriod) = ParseTaxPeriod(taxPeriod);
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var fy = await _fyRepo.GetActiveFinancialYearAsync(ct);
        var fyCode = fy?.Code ?? $"{startDate.Year}-{startDate.AddYears(1).ToString("yy")}";

        var allDocs = await _gstRepo.GetFinalizedDocumentsForPeriodAsync(company.Id, startDate, endDate, ct);
        var activeDocs = allDocs.Where(d => d.DocumentStatus != DocumentStatus.Cancelled).ToList();

        // Table 3.1(a): Outward Taxable Supplies (other than zero rated, nil rated, and reverse charge)
        var taxableDocs = activeDocs.Where(d =>
            !d.IsReverseCharge &&
            d.SupplyType != SupplyType.Export &&
            d.TotalTax > 0).ToList();

        var outTaxable = new Gstr3bTaxRowDto(
            TaxableValue: Math.Round(taxableDocs.Sum(d => d.SubTotalTaxable), 2),
            IntegratedTax: Math.Round(taxableDocs.Sum(d => d.IgstTotal), 2),
            CentralTax: Math.Round(taxableDocs.Sum(d => d.CgstTotal), 2),
            StateTax: Math.Round(taxableDocs.Sum(d => d.SgstTotal), 2),
            Cess: Math.Round(taxableDocs.Sum(d => d.CessTotal), 2)
        );

        // Table 3.1(b): Zero rated (Exports)
        var exportDocs = activeDocs.Where(d => d.SupplyType == SupplyType.Export).ToList();
        var zeroRated = new Gstr3bTaxRowDto(
            TaxableValue: Math.Round(exportDocs.Sum(d => d.SubTotalTaxable), 2),
            IntegratedTax: Math.Round(exportDocs.Sum(d => d.IgstTotal), 2),
            CentralTax: 0,
            StateTax: 0,
            Cess: Math.Round(exportDocs.Sum(d => d.CessTotal), 2)
        );

        // Table 3.1(c): Other Outward supplies (Nil rated, exempted)
        var nilExemptDocs = activeDocs.Where(d =>
            d.TotalTax == 0 &&
            d.SupplyType != SupplyType.Export &&
            !d.IsReverseCharge).ToList();
        var nilExemptValue = Math.Round(nilExemptDocs.Sum(d => d.SubTotalTaxable), 2);

        // Table 3.1(d): Inward supplies liable to reverse charge
        var rcmDocs = activeDocs.Where(d => d.IsReverseCharge).ToList();
        var rcmRow = new Gstr3bTaxRowDto(
            TaxableValue: Math.Round(rcmDocs.Sum(d => d.SubTotalTaxable), 2),
            IntegratedTax: Math.Round(rcmDocs.Sum(d => d.IgstTotal), 2),
            CentralTax: Math.Round(rcmDocs.Sum(d => d.CgstTotal), 2),
            StateTax: Math.Round(rcmDocs.Sum(d => d.SgstTotal), 2),
            Cess: Math.Round(rcmDocs.Sum(d => d.CessTotal), 2)
        );

        var table31 = new Gstr3bTable31Dto(
            OutwardTaxableSupplies: outTaxable,
            ZeroRatedSupplies: zeroRated,
            OtherOutwardNilExempt: nilExemptValue,
            InwardReverseChargeSupplies: rcmRow,
            NonGstOutwardSupplies: 0
        );

        // Table 3.2: Inter-state supplies to unregistered persons
        var b2cInterDocs = activeDocs.Where(d =>
            string.IsNullOrWhiteSpace(d.CustomerGstin) &&
            (d.SupplyType == SupplyType.InterState || d.PlaceOfSupplyStateCode != company.StateCode) &&
            d.IgstTotal > 0).ToList();

        var table32 = b2cInterDocs
            .GroupBy(d => d.PlaceOfSupplyStateCode)
            .Select(g => new Gstr3bTable32RowDto(
                PlaceOfSupplyStateCode: g.Key,
                StateName: GetStateName(g.Key),
                TaxableValue: Math.Round(g.Sum(d => d.SubTotalTaxable), 2),
                IntegratedTaxAmount: Math.Round(g.Sum(d => d.IgstTotal), 2)
            )).ToList();

        // Table 4: Eligible ITC (Inward RCM supplies liability eligible as credit + all other)
        var rcmItc = rcmRow.IntegratedTax + rcmRow.CentralTax + rcmRow.StateTax + rcmRow.Cess;
        var table4 = new Gstr3bTable4Dto(
            InwardReverseChargeItc: rcmItc,
            AllOtherItc: 0,
            NetItcAvailable: rcmItc
        );

        // Total Tax Liability
        var totalIgst = outTaxable.IntegratedTax + zeroRated.IntegratedTax + rcmRow.IntegratedTax;
        var totalCgst = outTaxable.CentralTax + rcmRow.CentralTax;
        var totalSgst = outTaxable.StateTax + rcmRow.StateTax;
        var totalCess = outTaxable.Cess + zeroRated.Cess + rcmRow.Cess;
        var grandTax = totalIgst + totalCgst + totalSgst + totalCess;

        var totalLiability = new Gstr3bTaxLiabilityDto(
            TaxableValue: outTaxable.TaxableValue + zeroRated.TaxableValue + rcmRow.TaxableValue + nilExemptValue,
            IntegratedTax: totalIgst,
            CentralTax: totalCgst,
            StateTax: totalSgst,
            Cess: totalCess,
            TotalTax: grandTax
        );

        return new Gstr3bSummaryDto(
            TaxPeriod: formattedPeriod,
            FinancialYear: fyCode,
            Table31: table31,
            Table32: table32,
            Table4: table4,
            TotalLiability: totalLiability
        );
    }

    public async Task<IReadOnlyList<HsnSummaryItemDto>> GetHsnSummaryAsync(string taxPeriod, CancellationToken ct = default)
    {
        var (startDate, endDate, _) = ParseTaxPeriod(taxPeriod);
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var allDocs = await _gstRepo.GetFinalizedDocumentsForPeriodAsync(company.Id, startDate, endDate, ct);
        var activeDocs = allDocs.Where(d => d.DocumentStatus != DocumentStatus.Cancelled).ToList();

        var hsnGroup = activeDocs
            .SelectMany(d => d.Items.Select(i => new
            {
                Hsn = string.IsNullOrWhiteSpace(i.HsnSacCode) ? "OTHER" : i.HsnSacCode.Trim(),
                Desc = i.ItemName.Trim(),
                Unit = string.IsNullOrWhiteSpace(i.UnitCode) ? "PCS" : i.UnitCode.Trim().ToUpperInvariant(),
                Qty = i.Quantity,
                Val = i.TotalAmount,
                Taxable = i.TaxableAmount,
                Rate = i.GstRatePercent,
                Cgst = i.CgstAmount,
                Sgst = i.SgstAmount,
                Igst = i.IgstAmount,
                Cess = i.CessAmount
            }))
            .GroupBy(x => new { x.Hsn, x.Rate })
            .Select(g => new HsnSummaryItemDto(
                HsnSacCode: g.Key.Hsn,
                Description: g.First().Desc,
                UnitCode: g.First().Unit,
                TotalQuantity: g.Sum(x => x.Qty),
                TotalValue: Math.Round(g.Sum(x => x.Val), 2),
                TaxableAmount: Math.Round(g.Sum(x => x.Taxable), 2),
                GstRatePercent: g.Key.Rate,
                CgstAmount: Math.Round(g.Sum(x => x.Cgst), 2),
                SgstAmount: Math.Round(g.Sum(x => x.Sgst), 2),
                IgstAmount: Math.Round(g.Sum(x => x.Igst), 2),
                CessAmount: Math.Round(g.Sum(x => x.Cess), 2)
            ))
            .OrderBy(h => h.HsnSacCode)
            .ToList();

        return hsnGroup;
    }

    public async Task<IReadOnlyList<GstReturnFilingDto>> GetFilingHistoryAsync(CancellationToken ct = default)
    {
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var filings = await _gstRepo.GetFilingHistoryAsync(company.Id, ct);
        return filings.Select(MapToFilingDto).ToList();
    }

    public async Task<GstReturnFilingDto> SubmitGstr1Async(string taxPeriod, bool simulate = true, CancellationToken ct = default)
    {
        var company = await _companyRepo.GetPrimaryCompanyAsync(ct)
            ?? throw new InvalidOperationException("Company profile not initialized.");

        var fy = await _fyRepo.GetActiveFinancialYearAsync(ct);
        var validation = await ValidateGstr1Async(taxPeriod, ct);

        var (startDate, _, formattedPeriod) = ParseTaxPeriod(taxPeriod);
        var fyCode = fy?.Code ?? $"{startDate.Year}-{startDate.AddYears(1).ToString("yy")}";

        var summary = await GetGstr1SummaryAsync(taxPeriod, ct);
        var jsonRoot = await GenerateGstr1JsonPayloadAsync(taxPeriod, ct);
        var jsonPayload = JsonSerializer.Serialize(jsonRoot);

        var filing = new GstReturnFiling
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ReturnType = GstReturnType.Gstr1,
            FinancialYear = fyCode,
            TaxPeriod = formattedPeriod,
            FilingStatus = validation.IsValid ? GstFilingStatus.ReadyForSubmission : GstFilingStatus.ValidationFailed,
            ValidationErrorsJson = validation.Errors.Count > 0 ? JsonSerializer.Serialize(validation.Errors) : null,
            PayloadJson = jsonPayload,
            TotalTaxableValue = summary.TotalTaxableValue,
            TotalTaxLiability = summary.TotalTaxLiability,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (!validation.IsValid)
        {
            var savedFailed = await _gstRepo.InsertAsync(filing, ct);
            _logger.LogWarning("GSTR-1 filing for {Period} marked ValidationFailed with {Count} errors", formattedPeriod, validation.Errors.Count);
            return MapToFilingDto(savedFailed);
        }

        filing.FilingStatus = GstFilingStatus.SubmittedToProvider;
        filing.ProviderName = _provider.ProviderName;

        var subResult = await _provider.SubmitReturnAsync(filing, ct);
        if (subResult.IsSuccess)
        {
            filing.FilingStatus = GstFilingStatus.Filed;
            filing.Arn = subResult.Arn;
            filing.ArnDate = subResult.AckDate;
            filing.ProviderReference = subResult.ReferenceId;
            filing.ProviderResponseJson = subResult.RawResponseJson;
            filing.FiledAtUtc = DateTime.UtcNow;
            _logger.LogInformation("GSTR-1 for {Period} successfully submitted. ARN: {Arn}", formattedPeriod, subResult.Arn);
        }
        else
        {
            filing.FilingStatus = GstFilingStatus.Rejected;
            filing.ProviderResponseJson = subResult.RawResponseJson ?? subResult.Message;
            _logger.LogError("GSTR-1 submission failed: {Msg}", subResult.Message);
        }

        var saved = await _gstRepo.InsertAsync(filing, ct);
        return MapToFilingDto(saved);
    }

    public async Task<GstReturnFilingDto> MarkAsFiledAsync(Guid filingId, string arn, DateTime arnDate, CancellationToken ct = default)
    {
        var filing = await _gstRepo.GetByIdAsync(filingId, ct)
            ?? throw new KeyNotFoundException($"Filing with ID {filingId} not found.");

        filing.FilingStatus = GstFilingStatus.Filed;
        filing.Arn = arn.Trim();
        filing.ArnDate = arnDate;
        filing.FiledAtUtc = DateTime.UtcNow;
        filing.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _gstRepo.UpdateAsync(filing, ct);
        _logger.LogInformation("Marked filing {Id} as manually Filed with ARN {Arn}", filingId, arn);
        return MapToFilingDto(updated);
    }

    private static (DateTime StartDate, DateTime EndDate, string FormattedPeriod) ParseTaxPeriod(string taxPeriod)
    {
        var raw = (taxPeriod ?? string.Empty).Trim().Replace("-", "").Replace("/", "");
        int month;
        int year;

        if (raw.Length == 6)
        {
            if (int.TryParse(raw.Substring(0, 2), out month) && int.TryParse(raw.Substring(2, 4), out year))
            {
                // MMYYYY format
            }
            else if (int.TryParse(raw.Substring(0, 4), out year) && int.TryParse(raw.Substring(4, 2), out month))
            {
                // YYYYMM format
            }
            else
            {
                month = DateTime.Today.Month;
                year = DateTime.Today.Year;
            }
        }
        else
        {
            month = DateTime.Today.Month;
            year = DateTime.Today.Year;
        }

        month = Math.Clamp(month, 1, 12);
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var formattedPeriod = $"{month:D2}{year}";

        return (startDate, endDate, formattedPeriod);
    }

    private static IReadOnlyList<Gstr1ItemDetailDto> GroupItemsByRate(IEnumerable<SalesDocumentItem> items)
    {
        return items
            .GroupBy(i => i.GstRatePercent)
            .Select(g => new Gstr1ItemDetailDto(
                RatePercent: g.Key,
                TaxableValue: Math.Round(g.Sum(x => x.TaxableAmount), 2),
                CgstAmount: Math.Round(g.Sum(x => x.CgstAmount), 2),
                SgstAmount: Math.Round(g.Sum(x => x.SgstAmount), 2),
                IgstAmount: Math.Round(g.Sum(x => x.IgstAmount), 2),
                CessAmount: Math.Round(g.Sum(x => x.CessAmount), 2)
            )).ToList();
    }

    private static string GetDocTypeName(DocumentType type) => type switch
    {
        DocumentType.TaxInvoice => "Tax Invoice",
        DocumentType.BillOfSupply => "Bill of Supply",
        DocumentType.Quotation => "Quotation",
        DocumentType.ProformaInvoice => "Proforma Invoice",
        DocumentType.CreditNote => "Credit Note",
        DocumentType.DebitNote => "Debit Note",
        DocumentType.DeliveryChallan => "Delivery Challan",
        _ => "Sales Document"
    };

    private static string GetStateName(string code) => code switch
    {
        "24" => "Gujarat",
        "27" => "Maharashtra",
        "07" => "Delhi",
        "29" => "Karnataka",
        "33" => "Tamil Nadu",
        "19" => "West Bengal",
        "09" => "Uttar Pradesh",
        "08" => "Rajasthan",
        "06" => "Haryana",
        "36" => "Telangana",
        "37" => "Andhra Pradesh",
        "32" => "Kerala",
        _ => $"State Code {code}"
    };

    private static GstReturnFilingDto MapToFilingDto(GstReturnFiling f)
    {
        IReadOnlyList<string> errors = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(f.ValidationErrorsJson))
        {
            try
            {
                errors = JsonSerializer.Deserialize<List<string>>(f.ValidationErrorsJson) ?? errors;
            }
            catch {}
        }

        return new GstReturnFilingDto(
            Id: f.Id,
            CompanyId: f.CompanyId,
            ReturnType: (GstReturnTypeDto)(int)f.ReturnType,
            FinancialYear: f.FinancialYear,
            TaxPeriod: f.TaxPeriod,
            FilingStatus: (GstFilingStatusDto)(int)f.FilingStatus,
            Arn: f.Arn,
            ArnDate: f.ArnDate,
            ValidationErrors: errors,
            ProviderName: f.ProviderName,
            ProviderReference: f.ProviderReference,
            TotalTaxableValue: f.TotalTaxableValue,
            TotalTaxLiability: f.TotalTaxLiability,
            CreatedAtUtc: f.CreatedAtUtc,
            FiledAtUtc: f.FiledAtUtc
        );
    }
}

