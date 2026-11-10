using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public interface ICompanyRepository
{
    Task<CompanyProfile?> GetPrimaryCompanyAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
    Task<CompanyProfile> CreateAsync(CompanyProfile company, CancellationToken cancellationToken = default);
    Task<CompanyProfile> UpdateAsync(CompanyProfile company, CancellationToken cancellationToken = default);
}

public interface IFinancialYearRepository
{
    Task<FinancialYear?> GetActiveFinancialYearAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialYear>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinancialYear> CreateAsync(FinancialYear financialYear, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> HasUsersAsync(CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, string[] roleNames, CancellationToken cancellationToken = default);
}
