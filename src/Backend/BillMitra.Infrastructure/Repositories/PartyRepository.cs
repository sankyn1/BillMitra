using System.Data;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class PartyRepository : IPartyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PartyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Party>> GetAllAsync(int? partyType = null, string? search = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT id, party_type AS PartyType, name AS Name, trade_name AS TradeName,
                   gstin AS Gstin, pan AS Pan, gst_registration_type AS GstRegistrationType,
                   state_code AS StateCode, state_name AS StateName,
                   address_line1 AS AddressLine1, address_line2 AS AddressLine2,
                   city AS City, pincode AS Pincode, phone AS Phone, email AS Email,
                   credit_limit AS CreditLimit, current_balance AS CurrentBalance,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM parties
            WHERE is_active = 1";

        if (partyType.HasValue)
        {
            sql += " AND (party_type = @PartyType OR party_type = 3)";
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (LOWER(name) LIKE @Search OR LOWER(trade_name) LIKE @Search OR LOWER(phone) LIKE @Search OR LOWER(gstin) LIKE @Search)";
        }
        sql += " ORDER BY name ASC";

        var list = await connection.QueryAsync<Party>(sql, new
        {
            PartyType = partyType,
            Search = $"%{search?.Trim().ToLowerInvariant()}%"
        });

        return list.AsList();
    }

    public async Task<Party?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, party_type AS PartyType, name AS Name, trade_name AS TradeName,
                   gstin AS Gstin, pan AS Pan, gst_registration_type AS GstRegistrationType,
                   state_code AS StateCode, state_name AS StateName,
                   address_line1 AS AddressLine1, address_line2 AS AddressLine2,
                   city AS City, pincode AS Pincode, phone AS Phone, email AS Email,
                   credit_limit AS CreditLimit, current_balance AS CurrentBalance,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM parties
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<Party>(sql, new { Id = id });
    }

    public async Task<Party> CreateAsync(Party p, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO parties (
                id, party_type, name, trade_name, gstin, pan, gst_registration_type,
                state_code, state_name, address_line1, address_line2, city, pincode,
                phone, email, credit_limit, current_balance, is_active, created_at_utc, updated_at_utc
            ) VALUES (
                @Id, @PartyType, @Name, @TradeName, @Gstin, @Pan, @GstRegistrationType,
                @StateCode, @StateName, @AddressLine1, @AddressLine2, @City, @Pincode,
                @Phone, @Email, @CreditLimit, @CurrentBalance, @IsActive, @CreatedAtUtc, @UpdatedAtUtc
            )";

        await connection.ExecuteAsync(sql, p);
        return p;
    }

    public async Task<Party> UpdateAsync(Party p, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE parties SET
                party_type = @PartyType,
                name = @Name,
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
                credit_limit = @CreditLimit,
                updated_at_utc = @UpdatedAtUtc
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, p);
        return p;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "UPDATE parties SET is_active = 0, updated_at_utc = @Now WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id, Now = DateTime.UtcNow });
    }
}

