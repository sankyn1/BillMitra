using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Purchases;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        ICompanyRepository companyRepository,
        ILogger<PurchaseService> logger)
    {
        _purchaseRepository = purchaseRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<PurchaseDocumentDto> CreateDraftAsync(CreateOrUpdatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (company == null)
            throw new InvalidOperationException("Company profile not initialized.");

        var doc = new PurchaseDocument
        {
            CompanyId = company.Id,
            SupplierId = request.SupplierId,
            SupplierName = request.SupplierName,
            SupplierGstin = request.SupplierGstin,
            SupplierAddress = request.SupplierAddress,
            SupplierStateCode = request.SupplierStateCode,
            PlaceOfSupplyStateCode = !string.IsNullOrWhiteSpace(request.PlaceOfSupplyStateCode) ? request.PlaceOfSupplyStateCode : company.StateCode,
            SupplyType = request.SupplierStateCode == (company.StateCode ?? "24") ? SupplyType.IntraState : SupplyType.InterState,
            DocumentType = (PurchaseDocumentType)request.DocumentType,
            DocumentStatus = PurchaseDocumentStatus.Draft,
            SupplierInvoiceNumber = request.SupplierInvoiceNumber,
            SupplierInvoiceDate = request.SupplierInvoiceDate,
            DocumentDate = request.DocumentDate,
            DueDate = request.DueDate,
            IsReverseCharge = request.IsReverseCharge,
            DefaultItcEligibility = (ItcEligibility)request.DefaultItcEligibility,
            Notes = request.Notes
        };

        PopulateItemsAndTotals(doc, request.Items, company.StateCode ?? "24");
        await _purchaseRepository.InsertAsync(doc, cancellationToken);
        _logger.LogInformation("Saved purchase document draft {SupplierInv} from {Supplier}", doc.SupplierInvoiceNumber, doc.SupplierName);

        return ToDto(doc);
    }

    public async Task<PurchaseDocumentDto> UpdateDraftAsync(Guid id, CreateOrUpdatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (doc == null)
            throw new InvalidOperationException($"Purchase document {id} not found.");

        if (doc.DocumentStatus != PurchaseDocumentStatus.Draft)
            throw new InvalidOperationException("Only draft purchase documents can be modified.");

        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        doc.SupplierId = request.SupplierId;
        doc.SupplierName = request.SupplierName;
        doc.SupplierGstin = request.SupplierGstin;
        doc.SupplierAddress = request.SupplierAddress;
        doc.SupplierStateCode = request.SupplierStateCode;
        doc.PlaceOfSupplyStateCode = request.PlaceOfSupplyStateCode;
        doc.SupplyType = request.SupplierStateCode == (company?.StateCode ?? "24") ? SupplyType.IntraState : SupplyType.InterState;
        doc.SupplierInvoiceNumber = request.SupplierInvoiceNumber;
        doc.SupplierInvoiceDate = request.SupplierInvoiceDate;
        doc.DocumentDate = request.DocumentDate;
        doc.DueDate = request.DueDate;
        doc.IsReverseCharge = request.IsReverseCharge;
        doc.DefaultItcEligibility = (ItcEligibility)request.DefaultItcEligibility;
        doc.Notes = request.Notes;

        PopulateItemsAndTotals(doc, request.Items, company?.StateCode ?? "24");
        await _purchaseRepository.UpdateAsync(doc, cancellationToken);

        return ToDto(doc);
    }

    public async Task<PurchaseDocumentDto> FinalizeAsync(Guid id, FinalizePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (doc == null)
            throw new InvalidOperationException($"Purchase document {id} not found.");

        if (doc.DocumentStatus != PurchaseDocumentStatus.Draft)
            throw new InvalidOperationException("Only draft purchase documents can be finalized.");

        if (doc.Items.Count == 0)
            throw new InvalidOperationException("Cannot finalize purchase document without line items.");

        doc.DocumentStatus = PurchaseDocumentStatus.Finalized;
        doc.FinalizedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            doc.Notes = request.Notes;
        }

        await _purchaseRepository.UpdateAsync(doc, cancellationToken);
        _logger.LogInformation("Finalized purchase invoice {SupplierInv} from {Supplier} (Total: {Total})", doc.SupplierInvoiceNumber, doc.SupplierName, doc.GrandTotal);

        return ToDto(doc);
    }

    public async Task<PurchaseDocumentDto> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var doc = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (doc == null)
            throw new InvalidOperationException($"Purchase document {id} not found.");

        if (!doc.CanCancel())
            throw new InvalidOperationException("Cannot cancel a purchase document that has recorded payments.");

        doc.DocumentStatus = PurchaseDocumentStatus.Cancelled;
        doc.CancelledAtUtc = DateTime.UtcNow;
        doc.CancellationReason = reason;

        await _purchaseRepository.UpdateAsync(doc, cancellationToken);
        return ToDto(doc);
    }

    public async Task<PurchaseDocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        return doc == null ? null : ToDto(doc);
    }

    public async Task<IReadOnlyList<PurchaseDocumentDto>> ListAsync(int? documentType = null, int? status = null, Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        var docs = await _purchaseRepository.ListAsync(documentType, status, supplierId, cancellationToken);
        return docs.Select(ToDto).ToList();
    }

    private static void PopulateItemsAndTotals(PurchaseDocument doc, List<PurchaseDocumentItemInput> inputs, string companyStateCode)
    {
        doc.Items.Clear();
        decimal subTotal = 0;
        decimal cgstTotal = 0;
        decimal sgstTotal = 0;
        decimal igstTotal = 0;

        var isIntraState = doc.SupplierStateCode == companyStateCode;

        foreach (var inp in inputs)
        {
            var gross = inp.Quantity * inp.UnitPrice;
            var disc = gross * (inp.DiscountPercent / 100m);
            var taxable = gross - disc;

            decimal cgstRate = 0, cgstAmt = 0;
            decimal sgstRate = 0, sgstAmt = 0;
            decimal igstRate = 0, igstAmt = 0;

            if (isIntraState)
            {
                cgstRate = inp.GstRate / 2m;
                sgstRate = inp.GstRate / 2m;
                cgstAmt = Math.Round(taxable * (cgstRate / 100m), 2, MidpointRounding.AwayFromZero);
                sgstAmt = Math.Round(taxable * (sgstRate / 100m), 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                igstRate = inp.GstRate;
                igstAmt = Math.Round(taxable * (igstRate / 100m), 2, MidpointRounding.AwayFromZero);
            }

            var itemTotal = taxable + cgstAmt + sgstAmt + igstAmt;

            var item = new PurchaseDocumentItem
            {
                PurchaseDocumentId = doc.Id,
                ProductId = inp.ProductId,
                ItemName = inp.ItemName,
                HsnCode = inp.HsnCode,
                Quantity = inp.Quantity,
                Unit = inp.Unit,
                UnitPrice = inp.UnitPrice,
                DiscountPercent = inp.DiscountPercent,
                DiscountAmount = disc,
                GstRate = inp.GstRate,
                TaxableAmount = taxable,
                CgstRate = cgstRate,
                CgstAmount = cgstAmt,
                SgstRate = sgstRate,
                SgstAmount = sgstAmt,
                IgstRate = igstRate,
                IgstAmount = igstAmt,
                TotalAmount = itemTotal,
                ItcEligibility = (ItcEligibility)inp.ItcEligibility
            };

            doc.Items.Add(item);

            subTotal += taxable;
            cgstTotal += cgstAmt;
            sgstTotal += sgstAmt;
            igstTotal += igstAmt;
        }

        var totalTax = cgstTotal + sgstTotal + igstTotal;
        var rawGrand = subTotal + totalTax;
        var roundedGrand = Math.Round(rawGrand, 0, MidpointRounding.AwayFromZero);

        doc.SubTotalTaxable = subTotal;
        doc.CgstTotal = cgstTotal;
        doc.SgstTotal = sgstTotal;
        doc.IgstTotal = igstTotal;
        doc.TotalTax = totalTax;
        doc.RoundOffAmount = roundedGrand - rawGrand;
        doc.GrandTotal = roundedGrand;
    }

    private static PurchaseDocumentDto ToDto(PurchaseDocument d) => new(
        d.Id,
        (int)d.DocumentType,
        d.DocumentType.ToString(),
        (int)d.DocumentStatus,
        d.DocumentStatus.ToString(),
        d.SupplierId,
        d.SupplierName,
        d.SupplierGstin,
        d.SupplierAddress,
        d.SupplierStateCode,
        d.PlaceOfSupplyStateCode,
        d.SupplierInvoiceNumber,
        d.SupplierInvoiceDate,
        d.DocumentDate,
        d.DueDate,
        d.IsReverseCharge,
        (int)d.DefaultItcEligibility,
        d.SubTotalTaxable,
        d.CgstTotal,
        d.SgstTotal,
        d.IgstTotal,
        d.CessTotal,
        d.TotalTax,
        d.RoundOffAmount,
        d.GrandTotal,
        d.PaidAmount,
        d.OutstandingBalance,
        d.Notes,
        d.CreatedAtUtc,
        d.FinalizedAtUtc,
        d.Items.Select(i => new PurchaseDocumentItemDto(
            i.Id, i.ProductId, i.ItemName, i.HsnCode, i.Quantity, i.Unit,
            i.UnitPrice, i.DiscountPercent, i.DiscountAmount, i.GstRate,
            i.TaxableAmount, i.CgstRate, i.CgstAmount, i.SgstRate, i.SgstAmount,
            i.IgstRate, i.IgstAmount, i.CessRate, i.CessAmount, i.TotalAmount,
            (int)i.ItcEligibility, i.ItcEligibility.ToString()
        )).ToList()
    );
}

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(
        IExpenseRepository expenseRepository,
        ICompanyRepository companyRepository,
        ILogger<ExpenseService> logger)
    {
        _expenseRepository = expenseRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var cats = await _expenseRepository.GetCategoriesAsync(cancellationToken);
        return cats.Select(c => new ExpenseCategoryDto(c.Id, c.Name, c.Description, c.IsActive)).ToList();
    }

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        var cat = new ExpenseCategory
        {
            Name = name,
            Description = description,
            IsActive = true
        };
        await _expenseRepository.CreateCategoryAsync(cat, cancellationToken);
        return new ExpenseCategoryDto(cat.Id, cat.Name, cat.Description, cat.IsActive);
    }

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (company == null)
            throw new InvalidOperationException("Company profile not initialized.");

        var cat = await _expenseRepository.GetCategoryByIdAsync(request.ExpenseCategoryId, cancellationToken);
        if (cat == null)
            throw new InvalidOperationException($"Expense category {request.ExpenseCategoryId} not found.");

        var voucherNo = await _expenseRepository.GetNextVoucherNumberAsync(cancellationToken);
        var gstAmt = Math.Round(request.Amount * (request.GstRate / 100m), 2, MidpointRounding.AwayFromZero);

        var expense = new ExpenseEntry
        {
            CompanyId = company.Id,
            ExpenseCategoryId = cat.Id,
            ExpenseCategoryName = cat.Name,
            VoucherNumber = voucherNo,
            ExpenseDate = request.ExpenseDate,
            Amount = request.Amount,
            GstRate = request.GstRate,
            GstAmount = gstAmt,
            IsItcClaimable = request.IsItcClaimable,
            PaymentMode = (PaymentMode)request.PaymentMode,
            ReferenceNumber = request.ReferenceNumber,
            PayeeName = request.PayeeName,
            Notes = request.Notes
        };

        await _expenseRepository.InsertAsync(expense, cancellationToken);
        _logger.LogInformation("Recorded expense voucher {Voucher} ({Category}: {Total})", voucherNo, cat.Name, expense.TotalAmount);

        return ToDto(expense);
    }

    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenseRepository.ListAsync(fromDate, toDate, categoryId, cancellationToken);
        return expenses.Select(ToDto).ToList();
    }

    public async Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        return expense == null ? null : ToDto(expense);
    }

    private static ExpenseDto ToDto(ExpenseEntry e) => new(
        e.Id,
        e.ExpenseCategoryId,
        e.ExpenseCategoryName,
        e.VoucherNumber,
        e.ExpenseDate,
        e.Amount,
        e.GstRate,
        e.GstAmount,
        e.TotalAmount,
        e.IsItcClaimable,
        (int)e.PaymentMode,
        e.PaymentMode.ToString(),
        e.ReferenceNumber,
        e.PayeeName,
        e.Notes,
        e.CreatedAtUtc
    );
}

