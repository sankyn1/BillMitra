using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PurchaseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> InsertAsync(PurchaseDocument doc, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var tran = conn.BeginTransaction();

        const string docSql = @"
INSERT INTO purchase_documents (
    id, company_id, supplier_id, supplier_name, supplier_gstin, supplier_address,
    supplier_state_code, place_of_supply_state_code, supply_type, document_type, document_status,
    purchase_order_number, supplier_invoice_number, supplier_invoice_date, document_date, due_date,
    is_reverse_charge, default_itc_eligibility, sub_total_taxable, cgst_total, sgst_total,
    igst_total, cess_total, total_tax, round_off_amount, grand_total, paid_amount,
    notes, created_at_utc, finalized_at_utc, cancelled_at_utc, cancellation_reason
) VALUES (
    @Id, @CompanyId, @SupplierId, @SupplierName, @SupplierGstin, @SupplierAddress,
    @SupplierStateCode, @PlaceOfSupplyStateCode, @SupplyType, @DocumentType, @DocumentStatus,
    @PurchaseOrderNumber, @SupplierInvoiceNumber, @SupplierInvoiceDate, @DocumentDate, @DueDate,
    @IsReverseCharge, @DefaultItcEligibility, @SubTotalTaxable, @CgstTotal, @SgstTotal,
    @IgstTotal, @CessTotal, @TotalTax, @RoundOffAmount, @GrandTotal, @PaidAmount,
    @Notes, @CreatedAtUtc, @FinalizedAtUtc, @CancelledAtUtc, @CancellationReason
);";

        await conn.ExecuteAsync(docSql, new
        {
            doc.Id,
            doc.CompanyId,
            doc.SupplierId,
            doc.SupplierName,
            doc.SupplierGstin,
            doc.SupplierAddress,
            doc.SupplierStateCode,
            doc.PlaceOfSupplyStateCode,
            SupplyType = (int)doc.SupplyType,
            DocumentType = (int)doc.DocumentType,
            DocumentStatus = (int)doc.DocumentStatus,
            doc.PurchaseOrderNumber,
            doc.SupplierInvoiceNumber,
            doc.SupplierInvoiceDate,
            doc.DocumentDate,
            doc.DueDate,
            doc.IsReverseCharge,
            DefaultItcEligibility = (int)doc.DefaultItcEligibility,
            doc.SubTotalTaxable,
            doc.CgstTotal,
            doc.SgstTotal,
            doc.IgstTotal,
            doc.CessTotal,
            doc.TotalTax,
            doc.RoundOffAmount,
            doc.GrandTotal,
            doc.PaidAmount,
            doc.Notes,
            doc.CreatedAtUtc,
            doc.FinalizedAtUtc,
            doc.CancelledAtUtc,
            doc.CancellationReason
        }, tran);

        const string itemSql = @"
INSERT INTO purchase_document_items (
    id, purchase_document_id, product_id, item_name, hsn_code, quantity, unit,
    unit_price, discount_percent, discount_amount, gst_rate, taxable_amount,
    cgst_rate, cgst_amount, sgst_rate, sgst_amount, igst_rate, igst_amount,
    cess_rate, cess_amount, total_amount, itc_eligibility
) VALUES (
    @Id, @PurchaseDocumentId, @ProductId, @ItemName, @HsnCode, @Quantity, @Unit,
    @UnitPrice, @DiscountPercent, @DiscountAmount, @GstRate, @TaxableAmount,
    @CgstRate, @CgstAmount, @SgstRate, @SgstAmount, @IgstRate, @IgstAmount,
    @CessRate, @CessAmount, @TotalAmount, @ItcEligibility
);";

        foreach (var item in doc.Items)
        {
            item.PurchaseDocumentId = doc.Id;
            await conn.ExecuteAsync(itemSql, new
            {
                item.Id,
                item.PurchaseDocumentId,
                item.ProductId,
                item.ItemName,
                item.HsnCode,
                item.Quantity,
                item.Unit,
                item.UnitPrice,
                item.DiscountPercent,
                item.DiscountAmount,
                item.GstRate,
                item.TaxableAmount,
                item.CgstRate,
                item.CgstAmount,
                item.SgstRate,
                item.SgstAmount,
                item.IgstRate,
                item.IgstAmount,
                item.CessRate,
                item.CessAmount,
                item.TotalAmount,
                ItcEligibility = (int)item.ItcEligibility
            }, tran);
        }

        tran.Commit();
        return doc.Id;
    }

    public async Task UpdateAsync(PurchaseDocument doc, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var tran = conn.BeginTransaction();

        const string docSql = @"
UPDATE purchase_documents SET
    supplier_name = @SupplierName,
    supplier_gstin = @SupplierGstin,
    supplier_address = @SupplierAddress,
    supplier_state_code = @SupplierStateCode,
    place_of_supply_state_code = @PlaceOfSupplyStateCode,
    supply_type = @SupplyType,
    document_type = @DocumentType,
    document_status = @DocumentStatus,
    purchase_order_number = @PurchaseOrderNumber,
    supplier_invoice_number = @SupplierInvoiceNumber,
    supplier_invoice_date = @SupplierInvoiceDate,
    document_date = @DocumentDate,
    due_date = @DueDate,
    is_reverse_charge = @IsReverseCharge,
    default_itc_eligibility = @DefaultItcEligibility,
    sub_total_taxable = @SubTotalTaxable,
    cgst_total = @CgstTotal,
    sgst_total = @SgstTotal,
    igst_total = @IgstTotal,
    cess_total = @CessTotal,
    total_tax = @TotalTax,
    round_off_amount = @RoundOffAmount,
    grand_total = @GrandTotal,
    paid_amount = @PaidAmount,
    notes = @Notes,
    finalized_at_utc = @FinalizedAtUtc,
    cancelled_at_utc = @CancelledAtUtc,
    cancellation_reason = @CancellationReason
WHERE id = @Id;";

        await conn.ExecuteAsync(docSql, new
        {
            doc.Id,
            doc.SupplierName,
            doc.SupplierGstin,
            doc.SupplierAddress,
            doc.SupplierStateCode,
            doc.PlaceOfSupplyStateCode,
            SupplyType = (int)doc.SupplyType,
            DocumentType = (int)doc.DocumentType,
            DocumentStatus = (int)doc.DocumentStatus,
            doc.PurchaseOrderNumber,
            doc.SupplierInvoiceNumber,
            doc.SupplierInvoiceDate,
            doc.DocumentDate,
            doc.DueDate,
            doc.IsReverseCharge,
            DefaultItcEligibility = (int)doc.DefaultItcEligibility,
            doc.SubTotalTaxable,
            doc.CgstTotal,
            doc.SgstTotal,
            doc.IgstTotal,
            doc.CessTotal,
            doc.TotalTax,
            doc.RoundOffAmount,
            doc.GrandTotal,
            doc.PaidAmount,
            doc.Notes,
            doc.FinalizedAtUtc,
            doc.CancelledAtUtc,
            doc.CancellationReason
        }, tran);

        if (doc.Items.Count > 0)
        {
            await conn.ExecuteAsync("DELETE FROM purchase_document_items WHERE purchase_document_id = @Id;", new { doc.Id }, tran);

            const string itemSql = @"
INSERT INTO purchase_document_items (
    id, purchase_document_id, product_id, item_name, hsn_code, quantity, unit,
    unit_price, discount_percent, discount_amount, gst_rate, taxable_amount,
    cgst_rate, cgst_amount, sgst_rate, sgst_amount, igst_rate, igst_amount,
    cess_rate, cess_amount, total_amount, itc_eligibility
) VALUES (
    @Id, @PurchaseDocumentId, @ProductId, @ItemName, @HsnCode, @Quantity, @Unit,
    @UnitPrice, @DiscountPercent, @DiscountAmount, @GstRate, @TaxableAmount,
    @CgstRate, @CgstAmount, @SgstRate, @SgstAmount, @IgstRate, @IgstAmount,
    @CessRate, @CessAmount, @TotalAmount, @ItcEligibility
);";

            foreach (var item in doc.Items)
            {
                item.PurchaseDocumentId = doc.Id;
                await conn.ExecuteAsync(itemSql, new
                {
                    item.Id,
                    item.PurchaseDocumentId,
                    item.ProductId,
                    item.ItemName,
                    item.HsnCode,
                    item.Quantity,
                    item.Unit,
                    item.UnitPrice,
                    item.DiscountPercent,
                    item.DiscountAmount,
                    item.GstRate,
                    item.TaxableAmount,
                    item.CgstRate,
                    item.CgstAmount,
                    item.SgstRate,
                    item.SgstAmount,
                    item.IgstRate,
                    item.IgstAmount,
                    item.CessRate,
                    item.CessAmount,
                    item.TotalAmount,
                    ItcEligibility = (int)item.ItcEligibility
                }, tran);
            }
        }

        tran.Commit();
    }

    public async Task<PurchaseDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string docSql = "SELECT * FROM purchase_documents WHERE id = @Id;";
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(docSql, new { Id = id });
        if (row == null) return null;

        var doc = MapDocument(row);

        const string itemSql = "SELECT * FROM purchase_document_items WHERE purchase_document_id = @Id;";
        var itemRows = await conn.QueryAsync<dynamic>(itemSql, new { Id = id });
        doc.Items = itemRows.Select(MapItem).ToList();

        return doc;
    }

    public async Task<IReadOnlyList<PurchaseDocument>> ListAsync(int? documentType = null, int? status = null, Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
SELECT * FROM purchase_documents
WHERE (@DocumentType IS NULL OR document_type = @DocumentType)
  AND (@Status IS NULL OR document_status = @Status)
  AND (@SupplierId IS NULL OR supplier_id = @SupplierId)
ORDER BY document_date DESC, created_at_utc DESC;";

        var rows = await conn.QueryAsync<dynamic>(sql, new { DocumentType = documentType, Status = status, SupplierId = supplierId });
        return rows.Select(MapDocument).ToList();
    }

    public async Task<decimal> GetSupplierOutstandingAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
SELECT COALESCE(SUM(grand_total - paid_amount), 0)
FROM purchase_documents
WHERE supplier_id = @SupplierId AND document_status IN (2, 3);";

        return await conn.ExecuteScalarAsync<decimal>(sql, new { SupplierId = supplierId });
    }

    private static PurchaseDocument MapDocument(dynamic row)
    {
        return new PurchaseDocument
        {
            Id = Guid.Parse(row.id.ToString()),
            CompanyId = Guid.Parse(row.company_id.ToString()),
            SupplierId = row.supplier_id != null ? Guid.Parse(row.supplier_id.ToString()) : null,
            SupplierName = row.supplier_name ?? string.Empty,
            SupplierGstin = row.supplier_gstin,
            SupplierAddress = row.supplier_address,
            SupplierStateCode = row.supplier_state_code ?? string.Empty,
            PlaceOfSupplyStateCode = row.place_of_supply_state_code ?? string.Empty,
            SupplyType = (SupplyType)(int)row.supply_type,
            DocumentType = (PurchaseDocumentType)(int)row.document_type,
            DocumentStatus = (PurchaseDocumentStatus)(int)row.document_status,
            PurchaseOrderNumber = row.purchase_order_number,
            SupplierInvoiceNumber = row.supplier_invoice_number ?? string.Empty,
            SupplierInvoiceDate = DateTime.Parse(row.supplier_invoice_date.ToString()),
            DocumentDate = DateTime.Parse(row.document_date.ToString()),
            DueDate = row.due_date != null ? DateTime.Parse(row.due_date.ToString()) : null,
            IsReverseCharge = row.is_reverse_charge is bool b ? b : (int)row.is_reverse_charge == 1,
            DefaultItcEligibility = (ItcEligibility)(int)row.default_itc_eligibility,
            SubTotalTaxable = (decimal)row.sub_total_taxable,
            CgstTotal = (decimal)row.cgst_total,
            SgstTotal = (decimal)row.sgst_total,
            IgstTotal = (decimal)row.igst_total,
            CessTotal = (decimal)row.cess_total,
            TotalTax = (decimal)row.total_tax,
            RoundOffAmount = (decimal)row.round_off_amount,
            GrandTotal = (decimal)row.grand_total,
            PaidAmount = (decimal)row.paid_amount,
            Notes = row.notes,
            CreatedAtUtc = DateTime.Parse(row.created_at_utc.ToString()),
            FinalizedAtUtc = row.finalized_at_utc != null ? DateTime.Parse(row.finalized_at_utc.ToString()) : null,
            CancelledAtUtc = row.cancelled_at_utc != null ? DateTime.Parse(row.cancelled_at_utc.ToString()) : null,
            CancellationReason = row.cancellation_reason
        };
    }

    private static PurchaseDocumentItem MapItem(dynamic row)
    {
        return new PurchaseDocumentItem
        {
            Id = Guid.Parse(row.id.ToString()),
            PurchaseDocumentId = Guid.Parse(row.purchase_document_id.ToString()),
            ProductId = row.product_id != null ? Guid.Parse(row.product_id.ToString()) : null,
            ItemName = row.item_name ?? string.Empty,
            HsnCode = row.hsn_code ?? string.Empty,
            Quantity = (decimal)row.quantity,
            Unit = row.unit ?? "NOS",
            UnitPrice = (decimal)row.unit_price,
            DiscountPercent = (decimal)row.discount_percent,
            DiscountAmount = (decimal)row.discount_amount,
            GstRate = (decimal)row.gst_rate,
            TaxableAmount = (decimal)row.taxable_amount,
            CgstRate = (decimal)row.cgst_rate,
            CgstAmount = (decimal)row.cgst_amount,
            SgstRate = (decimal)row.sgst_rate,
            SgstAmount = (decimal)row.sgst_amount,
            IgstRate = (decimal)row.igst_rate,
            IgstAmount = (decimal)row.igst_amount,
            CessRate = (decimal)row.cess_rate,
            CessAmount = (decimal)row.cess_amount,
            TotalAmount = (decimal)row.total_amount,
            ItcEligibility = (ItcEligibility)(int)row.itc_eligibility
        };
    }
}

public class ExpenseRepository : IExpenseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExpenseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM expense_categories WHERE is_active = 1 ORDER BY name ASC;";
        var rows = await conn.QueryAsync<dynamic>(sql);
        return rows.Select(r => new ExpenseCategory
        {
            Id = Guid.Parse(r.id.ToString()),
            Name = r.name,
            Description = r.description,
            IsActive = r.is_active is bool b ? b : (int)r.is_active == 1,
            CreatedAtUtc = DateTime.Parse(r.created_at_utc.ToString())
        }).ToList();
    }

    public async Task<ExpenseCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM expense_categories WHERE id = @Id;";
        var r = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (r == null) return null;
        return new ExpenseCategory
        {
            Id = Guid.Parse(r.id.ToString()),
            Name = r.name,
            Description = r.description,
            IsActive = r.is_active is bool b ? b : (int)r.is_active == 1,
            CreatedAtUtc = DateTime.Parse(r.created_at_utc.ToString())
        };
    }

    public async Task<ExpenseCategory> CreateCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "INSERT INTO expense_categories (id, name, description, is_active, created_at_utc) VALUES (@Id, @Name, @Description, @IsActive, @CreatedAtUtc);";
        await conn.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Description,
            IsActive = category.IsActive ? 1 : 0,
            category.CreatedAtUtc
        });
        return category;
    }

    public async Task<IReadOnlyList<ExpenseEntry>> ListAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
SELECT * FROM expense_entries
WHERE (@FromDate IS NULL OR expense_date >= @FromDate)
  AND (@ToDate IS NULL OR expense_date <= @ToDate)
  AND (@CategoryId IS NULL OR expense_category_id = @CategoryId)
ORDER BY expense_date DESC, created_at_utc DESC;";

        var rows = await conn.QueryAsync<dynamic>(sql, new { FromDate = fromDate, ToDate = toDate, CategoryId = categoryId });
        return rows.Select(r => new ExpenseEntry
        {
            Id = Guid.Parse(r.id.ToString()),
            CompanyId = Guid.Parse(r.company_id.ToString()),
            ExpenseCategoryId = Guid.Parse(r.expense_category_id.ToString()),
            ExpenseCategoryName = r.expense_category_name ?? string.Empty,
            VoucherNumber = r.voucher_number ?? string.Empty,
            ExpenseDate = DateTime.Parse(r.expense_date.ToString()),
            Amount = (decimal)r.amount,
            GstRate = (decimal)r.gst_rate,
            GstAmount = (decimal)r.gst_amount,
            IsItcClaimable = r.is_itc_claimable is bool b ? b : (int)r.is_itc_claimable == 1,
            PaymentMode = (PaymentMode)(int)r.payment_mode,
            ReferenceNumber = r.reference_number,
            PayeeName = r.payee_name,
            Notes = r.notes,
            CreatedAtUtc = DateTime.Parse(r.created_at_utc.ToString())
        }).ToList();
    }

    public async Task<ExpenseEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM expense_entries WHERE id = @Id;";
        var r = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (r == null) return null;

        return new ExpenseEntry
        {
            Id = Guid.Parse(r.id.ToString()),
            CompanyId = Guid.Parse(r.company_id.ToString()),
            ExpenseCategoryId = Guid.Parse(r.expense_category_id.ToString()),
            ExpenseCategoryName = r.expense_category_name ?? string.Empty,
            VoucherNumber = r.voucher_number ?? string.Empty,
            ExpenseDate = DateTime.Parse(r.expense_date.ToString()),
            Amount = (decimal)r.amount,
            GstRate = (decimal)r.gst_rate,
            GstAmount = (decimal)r.gst_amount,
            IsItcClaimable = r.is_itc_claimable is bool b ? b : (int)r.is_itc_claimable == 1,
            PaymentMode = (PaymentMode)(int)r.payment_mode,
            ReferenceNumber = r.reference_number,
            PayeeName = r.payee_name,
            Notes = r.notes,
            CreatedAtUtc = DateTime.Parse(r.created_at_utc.ToString())
        };
    }

    public async Task<Guid> InsertAsync(ExpenseEntry expense, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
INSERT INTO expense_entries (
    id, company_id, expense_category_id, expense_category_name, voucher_number,
    expense_date, amount, gst_rate, gst_amount, is_itc_claimable, payment_mode,
    reference_number, payee_name, notes, created_at_utc
) VALUES (
    @Id, @CompanyId, @ExpenseCategoryId, @ExpenseCategoryName, @VoucherNumber,
    @ExpenseDate, @Amount, @GstRate, @GstAmount, @IsItcClaimable, @PaymentMode,
    @ReferenceNumber, @PayeeName, @Notes, @CreatedAtUtc
);";

        await conn.ExecuteAsync(sql, new
        {
            expense.Id,
            expense.CompanyId,
            expense.ExpenseCategoryId,
            expense.ExpenseCategoryName,
            expense.VoucherNumber,
            expense.ExpenseDate,
            expense.Amount,
            expense.GstRate,
            expense.GstAmount,
            IsItcClaimable = expense.IsItcClaimable ? 1 : 0,
            PaymentMode = (int)expense.PaymentMode,
            expense.ReferenceNumber,
            expense.PayeeName,
            expense.Notes,
            expense.CreatedAtUtc
        });

        return expense.Id;
    }

    public async Task<string> GetNextVoucherNumberAsync(CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM expense_entries;";
        var count = await conn.ExecuteScalarAsync<int>(sql);
        return $"EXP/26-27/{(count + 1):D4}";
    }
}

