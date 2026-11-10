using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class GstReturnRepository : IGstReturnRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GstReturnRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GstReturnFiling> InsertAsync(GstReturnFiling filing, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
INSERT INTO gst_return_filings (
    id, company_id, return_type, financial_year, tax_period, filing_status,
    arn, arn_date, validation_errors_json, payload_json, provider_name,
    provider_reference, provider_response_json, total_taxable_value,
    total_tax_liability, created_at_utc, updated_at_utc, filed_at_utc
) VALUES (
    @Id, @CompanyId, @ReturnType, @FinancialYear, @TaxPeriod, @FilingStatus,
    @Arn, @ArnDate, @ValidationErrorsJson, @PayloadJson, @ProviderName,
    @ProviderReference, @ProviderResponseJson, @TotalTaxableValue,
    @TotalTaxLiability, @CreatedAtUtc, @UpdatedAtUtc, @FiledAtUtc
);";

        await conn.ExecuteAsync(sql, new
        {
            filing.Id,
            filing.CompanyId,
            ReturnType = (int)filing.ReturnType,
            filing.FinancialYear,
            filing.TaxPeriod,
            FilingStatus = (int)filing.FilingStatus,
            filing.Arn,
            filing.ArnDate,
            filing.ValidationErrorsJson,
            filing.PayloadJson,
            filing.ProviderName,
            filing.ProviderReference,
            filing.ProviderResponseJson,
            filing.TotalTaxableValue,
            filing.TotalTaxLiability,
            filing.CreatedAtUtc,
            filing.UpdatedAtUtc,
            filing.FiledAtUtc
        });

        return filing;
    }

    public async Task<GstReturnFiling> UpdateAsync(GstReturnFiling filing, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
UPDATE gst_return_filings SET
    filing_status = @FilingStatus,
    arn = @Arn,
    arn_date = @ArnDate,
    validation_errors_json = @ValidationErrorsJson,
    payload_json = @PayloadJson,
    provider_name = @ProviderName,
    provider_reference = @ProviderReference,
    provider_response_json = @ProviderResponseJson,
    total_taxable_value = @TotalTaxableValue,
    total_tax_liability = @TotalTaxLiability,
    updated_at_utc = @UpdatedAtUtc,
    filed_at_utc = @FiledAtUtc
WHERE id = @Id;";

        await conn.ExecuteAsync(sql, new
        {
            filing.Id,
            FilingStatus = (int)filing.FilingStatus,
            filing.Arn,
            filing.ArnDate,
            filing.ValidationErrorsJson,
            filing.PayloadJson,
            filing.ProviderName,
            filing.ProviderReference,
            filing.ProviderResponseJson,
            filing.TotalTaxableValue,
            filing.TotalTaxLiability,
            filing.UpdatedAtUtc,
            filing.FiledAtUtc
        });

        return filing;
    }

    public async Task<GstReturnFiling?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
SELECT 
    id as Id,
    company_id as CompanyId,
    return_type as ReturnType,
    financial_year as FinancialYear,
    tax_period as TaxPeriod,
    filing_status as FilingStatus,
    arn as Arn,
    arn_date as ArnDate,
    validation_errors_json as ValidationErrorsJson,
    payload_json as PayloadJson,
    provider_name as ProviderName,
    provider_reference as ProviderReference,
    provider_response_json as ProviderResponseJson,
    total_taxable_value as TotalTaxableValue,
    total_tax_liability as TotalTaxLiability,
    created_at_utc as CreatedAtUtc,
    updated_at_utc as UpdatedAtUtc,
    filed_at_utc as FiledAtUtc
FROM gst_return_filings
WHERE id = @Id;";

        return await conn.QuerySingleOrDefaultAsync<GstReturnFiling>(sql, new { Id = id });
    }

    public async Task<GstReturnFiling?> GetLatestFilingAsync(Guid companyId, GstReturnType returnType, string taxPeriod, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
SELECT 
    id as Id,
    company_id as CompanyId,
    return_type as ReturnType,
    financial_year as FinancialYear,
    tax_period as TaxPeriod,
    filing_status as FilingStatus,
    arn as Arn,
    arn_date as ArnDate,
    validation_errors_json as ValidationErrorsJson,
    payload_json as PayloadJson,
    provider_name as ProviderName,
    provider_reference as ProviderReference,
    provider_response_json as ProviderResponseJson,
    total_taxable_value as TotalTaxableValue,
    total_tax_liability as TotalTaxLiability,
    created_at_utc as CreatedAtUtc,
    updated_at_utc as UpdatedAtUtc,
    filed_at_utc as FiledAtUtc
FROM gst_return_filings
WHERE company_id = @CompanyId AND return_type = @ReturnType AND tax_period = @TaxPeriod
ORDER BY created_at_utc DESC;";

        return await conn.QueryFirstOrDefaultAsync<GstReturnFiling>(sql, new
        {
            CompanyId = companyId,
            ReturnType = (int)returnType,
            TaxPeriod = taxPeriod
        });
    }

    public async Task<IReadOnlyList<GstReturnFiling>> GetFilingHistoryAsync(Guid companyId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
SELECT 
    id as Id,
    company_id as CompanyId,
    return_type as ReturnType,
    financial_year as FinancialYear,
    tax_period as TaxPeriod,
    filing_status as FilingStatus,
    arn as Arn,
    arn_date as ArnDate,
    validation_errors_json as ValidationErrorsJson,
    payload_json as PayloadJson,
    provider_name as ProviderName,
    provider_reference as ProviderReference,
    provider_response_json as ProviderResponseJson,
    total_taxable_value as TotalTaxableValue,
    total_tax_liability as TotalTaxLiability,
    created_at_utc as CreatedAtUtc,
    updated_at_utc as UpdatedAtUtc,
    filed_at_utc as FiledAtUtc
FROM gst_return_filings
WHERE company_id = @CompanyId
ORDER BY created_at_utc DESC;";

        var list = await conn.QueryAsync<GstReturnFiling>(sql, new { CompanyId = companyId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<SalesDocument>> GetFinalizedDocumentsForPeriodAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

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
    source_document_id as SourceDocumentId,
    source_document_number as SourceDocumentNumber,
    converted_to_document_id as ConvertedToDocumentId,
    created_by as CreatedBy
FROM sales_documents
WHERE company_id = @CompanyId
  AND document_date >= @StartDate
  AND document_date <= @EndDate
  AND document_status IN (2, 3, 4, 5, 6)
ORDER BY document_date ASC, document_number ASC;";

        var docs = (await conn.QueryAsync<SalesDocument>(docSql, new
        {
            CompanyId = companyId,
            StartDate = startDate.Date,
            EndDate = endDate.Date
        })).ToList();

        if (docs.Count == 0) return docs;

        var docIds = docs.Select(d => d.Id).ToList();
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
WHERE sales_document_id IN @DocIds
ORDER BY line_number ASC;";

        var items = (await conn.QueryAsync<SalesDocumentItem>(itemsSql, new { DocIds = docIds })).ToList();
        var itemsByDoc = items.GroupBy(i => i.SalesDocumentId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var doc in docs)
        {
            if (itemsByDoc.TryGetValue(doc.Id, out var docItems))
            {
                doc.Items = docItems;
            }
        }

        return docs;
    }
}
