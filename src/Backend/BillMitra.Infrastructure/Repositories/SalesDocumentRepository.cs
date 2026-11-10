using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class SalesDocumentRepository : ISalesDocumentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SalesDocumentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SalesDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string docSql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_status as DocumentStatus,
    document_number as DocumentNumber,
    draft_number as DraftNumber,
    document_date as DocumentDate,
    due_date as DueDate,
    company_id as CompanyId,
    party_id as PartyId,
    customer_name as CustomerName,
    customer_gstin as CustomerGstin,
    billing_address as BillingAddress,
    shipping_address as ShippingAddress,
    place_of_supply_state_code as PlaceOfSupplyStateCode,
    supplier_state_code as SupplierStateCode,
    is_reverse_charge as IsReverseCharge,
    supply_type as SupplyType,
    currency_code as CurrencyCode,
    sub_total_taxable as SubTotalTaxable,
    cgst_total as CgstTotal,
    sgst_total as SgstTotal,
    igst_total as IgstTotal,
    cess_total as CessTotal,
    total_tax as TotalTax,
    round_off_amount as RoundOffAmount,
    grand_total as GrandTotal,
    paid_amount as PaidAmount,
    total_amount_in_words as TotalAmountInWords,
    notes as Notes,
    terms_and_conditions as TermsAndConditions,
    idempotency_key as IdempotencyKey,
    created_at_utc as CreatedAtUtc,
    finalized_at_utc as FinalizedAtUtc,
    cancelled_at_utc as CancelledAtUtc,
    cancellation_reason as CancellationReason,
    created_by as CreatedBy
FROM sales_documents
WHERE id = @Id;";

        var doc = await conn.QuerySingleOrDefaultAsync<SalesDocument>(docSql, new { Id = id });
        if (doc == null) return null;

        const string itemsSql = @"
SELECT 
    id as Id,
    sales_document_id as SalesDocumentId,
    product_id as ProductId,
    line_number as LineNumber,
    item_name as ItemName,
    hsn_sac_code as HsnSacCode,
    quantity as Quantity,
    unit_code as UnitCode,
    unit_price as UnitPrice,
    discount_percent as DiscountPercent,
    discount_amount as DiscountAmount,
    taxable_amount as TaxableAmount,
    gst_rate_percent as GstRatePercent,
    cgst_rate_percent as CgstRatePercent,
    cgst_amount as CgstAmount,
    sgst_rate_percent as SgstRatePercent,
    sgst_amount as SgstAmount,
    igst_rate_percent as IgstRatePercent,
    igst_amount as IgstAmount,
    cess_rate_percent as CessRatePercent,
    cess_amount as CessAmount,
    total_amount as TotalAmount
FROM sales_document_items
WHERE sales_document_id = @DocId
ORDER BY line_number ASC;";

        var items = await conn.QueryAsync<SalesDocumentItem>(itemsSql, new { DocId = id });
        doc.Items = items.ToList();

        return doc;
    }

    public async Task<IReadOnlyList<SalesDocument>> GetAllAsync(
        int? documentType = null,
        int? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? partyId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_status as DocumentStatus,
    document_number as DocumentNumber,
    draft_number as DraftNumber,
    document_date as DocumentDate,
    due_date as DueDate,
    company_id as CompanyId,
    party_id as PartyId,
    customer_name as CustomerName,
    customer_gstin as CustomerGstin,
    billing_address as BillingAddress,
    shipping_address as ShippingAddress,
    place_of_supply_state_code as PlaceOfSupplyStateCode,
    supplier_state_code as SupplierStateCode,
    is_reverse_charge as IsReverseCharge,
    supply_type as SupplyType,
    currency_code as CurrencyCode,
    sub_total_taxable as SubTotalTaxable,
    cgst_total as CgstTotal,
    sgst_total as SgstTotal,
    igst_total as IgstTotal,
    cess_total as CessTotal,
    total_tax as TotalTax,
    round_off_amount as RoundOffAmount,
    grand_total as GrandTotal,
    paid_amount as PaidAmount,
    total_amount_in_words as TotalAmountInWords,
    notes as Notes,
    terms_and_conditions as TermsAndConditions,
    idempotency_key as IdempotencyKey,
    created_at_utc as CreatedAtUtc,
    finalized_at_utc as FinalizedAtUtc,
    cancelled_at_utc as CancelledAtUtc,
    cancellation_reason as CancellationReason,
    created_by as CreatedBy
FROM sales_documents
WHERE 1=1";

        var parameters = new DynamicParameters();

        if (documentType.HasValue)
        {
            sql += " AND document_type = @DocType";
            parameters.Add("DocType", documentType.Value);
        }

        if (status.HasValue)
        {
            sql += " AND document_status = @Status";
            parameters.Add("Status", status.Value);
        }

        if (fromDate.HasValue)
        {
            sql += " AND document_date >= @FromDate";
            parameters.Add("FromDate", fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            sql += " AND document_date <= @ToDate";
            parameters.Add("ToDate", toDate.Value.Date);
        }

        if (partyId.HasValue)
        {
            sql += " AND party_id = @PartyId";
            parameters.Add("PartyId", partyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (customer_name LIKE @Search OR document_number LIKE @Search OR draft_number LIKE @Search)";
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        sql += " ORDER BY created_at_utc DESC;";

        var docs = await conn.QueryAsync<SalesDocument>(sql, parameters);
        return docs.ToList();
    }

    public async Task<SalesDocument> InsertAsync(SalesDocument doc, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        const string docSql = @"
INSERT INTO sales_documents (
    id, document_type, document_status, document_number, draft_number,
    document_date, due_date, company_id, party_id, customer_name,
    customer_gstin, billing_address, shipping_address, place_of_supply_state_code,
    supplier_state_code, is_reverse_charge, supply_type, currency_code,
    sub_total_taxable, cgst_total, sgst_total, igst_total, cess_total,
    total_tax, round_off_amount, grand_total, paid_amount, total_amount_in_words,
    notes, terms_and_conditions, idempotency_key, created_at_utc,
    finalized_at_utc, cancelled_at_utc, cancellation_reason, created_by
) VALUES (
    @Id, @DocumentType, @DocumentStatus, @DocumentNumber, @DraftNumber,
    @DocumentDate, @DueDate, @CompanyId, @PartyId, @CustomerName,
    @CustomerGstin, @BillingAddress, @ShippingAddress, @PlaceOfSupplyStateCode,
    @SupplierStateCode, @IsReverseCharge, @SupplyType, @CurrencyCode,
    @SubTotalTaxable, @CgstTotal, @SgstTotal, @IgstTotal, @CessTotal,
    @TotalTax, @RoundOffAmount, @GrandTotal, @PaidAmount, @TotalAmountInWords,
    @Notes, @TermsAndConditions, @IdempotencyKey, @CreatedAtUtc,
    @FinalizedAtUtc, @CancelledAtUtc, @CancellationReason, @CreatedBy
);";

        await conn.ExecuteAsync(docSql, new
        {
            doc.Id,
            DocumentType = (int)doc.DocumentType,
            DocumentStatus = (int)doc.DocumentStatus,
            doc.DocumentNumber,
            doc.DraftNumber,
            doc.DocumentDate,
            doc.DueDate,
            doc.CompanyId,
            doc.PartyId,
            doc.CustomerName,
            doc.CustomerGstin,
            doc.BillingAddress,
            doc.ShippingAddress,
            doc.PlaceOfSupplyStateCode,
            doc.SupplierStateCode,
            doc.IsReverseCharge,
            SupplyType = (int)doc.SupplyType,
            doc.CurrencyCode,
            doc.SubTotalTaxable,
            doc.CgstTotal,
            doc.SgstTotal,
            doc.IgstTotal,
            doc.CessTotal,
            doc.TotalTax,
            doc.RoundOffAmount,
            doc.GrandTotal,
            doc.PaidAmount,
            doc.TotalAmountInWords,
            doc.Notes,
            doc.TermsAndConditions,
            doc.IdempotencyKey,
            doc.CreatedAtUtc,
            doc.FinalizedAtUtc,
            doc.CancelledAtUtc,
            doc.CancellationReason,
            doc.CreatedBy
        }, tx);

        const string itemSql = @"
INSERT INTO sales_document_items (
    id, sales_document_id, product_id, line_number, item_name,
    hsn_sac_code, quantity, unit_code, unit_price, discount_percent,
    discount_amount, taxable_amount, gst_rate_percent, cgst_rate_percent,
    cgst_amount, sgst_rate_percent, sgst_amount, igst_rate_percent,
    igst_amount, cess_rate_percent, cess_amount, total_amount
) VALUES (
    @Id, @SalesDocumentId, @ProductId, @LineNumber, @ItemName,
    @HsnSacCode, @Quantity, @UnitCode, @UnitPrice, @DiscountPercent,
    @DiscountAmount, @TaxableAmount, @GstRatePercent, @CgstRatePercent,
    @CgstAmount, @SgstRatePercent, @SgstAmount, @IgstRatePercent,
    @IgstAmount, @CessRatePercent, @CessAmount, @TotalAmount
);";

        foreach (var item in doc.Items)
        {
            await conn.ExecuteAsync(itemSql, item, tx);
        }

        tx.Commit();
        return doc;
    }

    public async Task<SalesDocument> UpdateAsync(SalesDocument doc, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        const string docSql = @"
UPDATE sales_documents SET
    document_type = @DocumentType,
    document_status = @DocumentStatus,
    document_number = @DocumentNumber,
    draft_number = @DraftNumber,
    document_date = @DocumentDate,
    due_date = @DueDate,
    party_id = @PartyId,
    customer_name = @CustomerName,
    customer_gstin = @CustomerGstin,
    billing_address = @BillingAddress,
    shipping_address = @ShippingAddress,
    place_of_supply_state_code = @PlaceOfSupplyStateCode,
    supplier_state_code = @SupplierStateCode,
    is_reverse_charge = @IsReverseCharge,
    supply_type = @SupplyType,
    sub_total_taxable = @SubTotalTaxable,
    cgst_total = @CgstTotal,
    sgst_total = @SgstTotal,
    igst_total = @IgstTotal,
    cess_total = @CessTotal,
    total_tax = @TotalTax,
    round_off_amount = @RoundOffAmount,
    grand_total = @GrandTotal,
    paid_amount = @PaidAmount,
    total_amount_in_words = @TotalAmountInWords,
    notes = @Notes,
    terms_and_conditions = @TermsAndConditions,
    idempotency_key = @IdempotencyKey,
    finalized_at_utc = @FinalizedAtUtc,
    cancelled_at_utc = @CancelledAtUtc,
    cancellation_reason = @CancellationReason
WHERE id = @Id;";

        await conn.ExecuteAsync(docSql, new
        {
            doc.Id,
            DocumentType = (int)doc.DocumentType,
            DocumentStatus = (int)doc.DocumentStatus,
            doc.DocumentNumber,
            doc.DraftNumber,
            doc.DocumentDate,
            doc.DueDate,
            doc.PartyId,
            doc.CustomerName,
            doc.CustomerGstin,
            doc.BillingAddress,
            doc.ShippingAddress,
            doc.PlaceOfSupplyStateCode,
            doc.SupplierStateCode,
            doc.IsReverseCharge,
            SupplyType = (int)doc.SupplyType,
            doc.SubTotalTaxable,
            doc.CgstTotal,
            doc.SgstTotal,
            doc.IgstTotal,
            doc.CessTotal,
            doc.TotalTax,
            doc.RoundOffAmount,
            doc.GrandTotal,
            doc.PaidAmount,
            doc.TotalAmountInWords,
            doc.Notes,
            doc.TermsAndConditions,
            doc.IdempotencyKey,
            doc.FinalizedAtUtc,
            doc.CancelledAtUtc,
            doc.CancellationReason
        }, tx);

        // Replace line items
        await conn.ExecuteAsync("DELETE FROM sales_document_items WHERE sales_document_id = @DocId;", new { DocId = doc.Id }, tx);

        const string itemSql = @"
INSERT INTO sales_document_items (
    id, sales_document_id, product_id, line_number, item_name,
    hsn_sac_code, quantity, unit_code, unit_price, discount_percent,
    discount_amount, taxable_amount, gst_rate_percent, cgst_rate_percent,
    cgst_amount, sgst_rate_percent, sgst_amount, igst_rate_percent,
    igst_amount, cess_rate_percent, cess_amount, total_amount
) VALUES (
    @Id, @SalesDocumentId, @ProductId, @LineNumber, @ItemName,
    @HsnSacCode, @Quantity, @UnitCode, @UnitPrice, @DiscountPercent,
    @DiscountAmount, @TaxableAmount, @GstRatePercent, @CgstRatePercent,
    @CgstAmount, @SgstRatePercent, @SgstAmount, @IgstRatePercent,
    @IgstAmount, @CessRatePercent, @CessAmount, @TotalAmount
);";

        foreach (var item in doc.Items)
        {
            await conn.ExecuteAsync(itemSql, item, tx);
        }

        tx.Commit();
        return doc;
    }

    public async Task<SalesDocument?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id FROM sales_documents WHERE idempotency_key = @Key;";
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>(sql, new { Key = idempotencyKey });
        if (!id.HasValue) return null;
        return await GetByIdAsync(id.Value, cancellationToken);
    }
}

