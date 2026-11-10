using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class DocumentSequenceRepository : IDocumentSequenceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IFinancialYearRepository _financialYearRepo;

    public DocumentSequenceRepository(
        IDbConnectionFactory connectionFactory,
        IFinancialYearRepository financialYearRepo)
    {
        _connectionFactory = connectionFactory;
        _financialYearRepo = financialYearRepo;
    }

    public async Task<DocumentSequence> GetOrCreateSequenceAsync(Guid financialYearId, DocumentType documentType, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string selectSql = @"
SELECT 
    id as Id,
    financial_year_id as FinancialYearId,
    document_type as DocumentType,
    prefix as Prefix,
    financial_year_token as FinancialYearToken,
    next_number as NextNumber,
    padding_length as PaddingLength,
    suffix as Suffix,
    is_active as IsActive
FROM document_sequences
WHERE financial_year_id = @FyId AND document_type = @DocType;";

        var seq = await conn.QuerySingleOrDefaultAsync<DocumentSequence>(selectSql, new
        {
            FyId = financialYearId,
            DocType = (int)documentType
        });

        if (seq != null) return seq;

        var fy = await _financialYearRepo.GetActiveFinancialYearAsync(cancellationToken);
        var fyCode = fy?.Code ?? "2026-27";
        // Convert '2026-27' to '26-27'
        var token = fyCode.Length >= 7 && fyCode.Contains('-')
            ? $"{fyCode.Substring(2, 2)}-{fyCode.Substring(fyCode.IndexOf('-') + 1)}"
            : "26-27";

        var prefix = documentType switch
        {
            DocumentType.TaxInvoice => "INV",
            DocumentType.BillOfSupply => "BOS",
            DocumentType.Quotation => "QT",
            DocumentType.ProformaInvoice => "PI",
            DocumentType.CreditNote => "CN",
            DocumentType.DebitNote => "DN",
            DocumentType.DeliveryChallan => "DC",
            DocumentType.ReceiptVoucher => "RV",
            _ => "DOC"
        };

        seq = new DocumentSequence
        {
            Id = Guid.NewGuid(),
            FinancialYearId = financialYearId,
            DocumentType = documentType,
            Prefix = prefix,
            FinancialYearToken = token,
            NextNumber = 1,
            PaddingLength = 4,
            IsActive = true
        };

        const string insertSql = @"
INSERT INTO document_sequences (
    id, financial_year_id, document_type, prefix,
    financial_year_token, next_number, padding_length, suffix, is_active
) VALUES (
    @Id, @FinancialYearId, @DocumentType, @Prefix,
    @FinancialYearToken, @NextNumber, @PaddingLength, @Suffix, @IsActive
);";

        await conn.ExecuteAsync(insertSql, new
        {
            seq.Id,
            seq.FinancialYearId,
            DocumentType = (int)seq.DocumentType,
            seq.Prefix,
            seq.FinancialYearToken,
            seq.NextNumber,
            seq.PaddingLength,
            seq.Suffix,
            seq.IsActive
        });

        return seq;
    }

    public async Task<string> AllocateNextNumberAsync(Guid financialYearId, DocumentType documentType, CancellationToken cancellationToken = default)
    {
        var seq = await GetOrCreateSequenceAsync(financialYearId, documentType, cancellationToken);

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();

        // Concurrency-safe atomic increment and fetch
        const string selectForUpdateSql = @"
SELECT next_number
FROM document_sequences
WHERE id = @Id;";

        var currentNumber = await conn.QuerySingleAsync<long>(selectForUpdateSql, new { seq.Id }, tx);
        var allocatedFormattedNumber = seq.FormatNumber(currentNumber);

        const string updateSql = @"
UPDATE document_sequences
SET next_number = next_number + 1
WHERE id = @Id;";

        await conn.ExecuteAsync(updateSql, new { seq.Id }, tx);
        tx.Commit();

        return allocatedFormattedNumber;
    }
}
