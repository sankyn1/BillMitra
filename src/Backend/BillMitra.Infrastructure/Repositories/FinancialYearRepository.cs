using System.Data;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class FinancialYearRepository : IFinancialYearRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FinancialYearRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FinancialYear?> GetActiveFinancialYearAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, code AS Code, start_date AS StartDate, end_date AS EndDate,
                   is_current AS IsCurrent, is_closed AS IsClosed, created_at_utc AS CreatedAtUtc
            FROM financial_years
            WHERE is_current = 1
            ORDER BY created_at_utc DESC";

        var result = await connection.QueryFirstOrDefaultAsync<FinancialYear>(sql);
        return result;
    }

    public async Task<IReadOnlyList<FinancialYear>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, code AS Code, start_date AS StartDate, end_date AS EndDate,
                   is_current AS IsCurrent, is_closed AS IsClosed, created_at_utc AS CreatedAtUtc
            FROM financial_years
            ORDER BY start_date DESC";

        var list = await connection.QueryAsync<FinancialYear>(sql);
        return list.AsList();
    }

    public async Task<FinancialYear> CreateAsync(FinancialYear financialYear, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO financial_years (id, code, start_date, end_date, is_current, is_closed, created_at_utc)
            VALUES (@Id, @Code, @StartDate, @EndDate, @IsCurrent, @IsClosed, @CreatedAtUtc)";

        await connection.ExecuteAsync(sql, new
        {
            financialYear.Id,
            financialYear.Code,
            StartDate = financialYear.StartDate.ToString("yyyy-MM-dd"),
            EndDate = financialYear.EndDate.ToString("yyyy-MM-dd"),
            financialYear.IsCurrent,
            financialYear.IsClosed,
            financialYear.CreatedAtUtc
        });
        return financialYear;
    }

    public async Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE financial_years SET is_current = 0;
            UPDATE financial_years SET is_current = 1 WHERE id = @Id;";

        await connection.ExecuteAsync(sql, new { Id = id });
    }
}

