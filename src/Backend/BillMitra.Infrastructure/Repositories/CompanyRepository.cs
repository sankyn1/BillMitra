using System.Data;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CompanyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CompanyProfile?> GetPrimaryCompanyAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, legal_name AS LegalName, trade_name AS TradeName, gstin AS Gstin, pan AS Pan,
                   gst_registration_type AS GstRegistrationType, state_code AS StateCode, state_name AS StateName,
                   address_line1 AS AddressLine1, address_line2 AS AddressLine2, city AS City, pincode AS Pincode,
                   phone AS Phone, email AS Email, bank_name AS BankName, bank_account_number AS BankAccountNumber,
                   bank_ifsc AS BankIfsc, bank_branch AS BankBranch, upi_id AS UpiId,
                   invoice_footer_notes AS InvoiceFooterNotes, terms_and_conditions AS TermsAndConditions,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM company_profile
            ORDER BY created_at_utc ASC";

        var result = await connection.QueryFirstOrDefaultAsync<CompanyProfile>(sql);
        return result;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM company_profile";
        var count = await connection.ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task<CompanyProfile> CreateAsync(CompanyProfile company, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO company_profile (
                id, legal_name, trade_name, gstin, pan, gst_registration_type, state_code, state_name,
                address_line1, address_line2, city, pincode, phone, email,
                bank_name, bank_account_number, bank_ifsc, bank_branch, upi_id,
                invoice_footer_notes, terms_and_conditions, is_active, created_at_utc, updated_at_utc
            ) VALUES (
                @Id, @LegalName, @TradeName, @Gstin, @Pan, @GstRegistrationType, @StateCode, @StateName,
                @AddressLine1, @AddressLine2, @City, @Pincode, @Phone, @Email,
                @BankName, @BankAccountNumber, @BankIfsc, @BankBranch, @UpiId,
                @InvoiceFooterNotes, @TermsAndConditions, @IsActive, @CreatedAtUtc, @UpdatedAtUtc
            )";

        await connection.ExecuteAsync(sql, company);
        return company;
    }

    public async Task<CompanyProfile> UpdateAsync(CompanyProfile company, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE company_profile SET
                legal_name = @LegalName,
                trade_name = @TradeName,
                gstin = @Gstin,
                pan = @Pan,
                gst_registration_type = @GstRegistrationType,
                state_code = @StateCode,
                state_name = @StateName,
                address_line1 = @AddressLine1,
                address_line2 = @AddressLine2,
                city = @City,
                pincode = @Pincode,
                phone = @Phone,
                email = @Email,
                bank_name = @BankName,
                bank_account_number = @BankAccountNumber,
                bank_ifsc = @BankIfsc,
                bank_branch = @BankBranch,
                upi_id = @UpiId,
                invoice_footer_notes = @InvoiceFooterNotes,
                terms_and_conditions = @TermsAndConditions,
                is_active = @IsActive,
                updated_at_utc = @UpdatedAtUtc
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, company);
        return company;
    }
}

