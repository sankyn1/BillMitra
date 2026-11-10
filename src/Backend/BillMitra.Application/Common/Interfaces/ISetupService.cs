using BillMitra.Contracts.Company;
using BillMitra.Contracts.FinancialYear;
using BillMitra.Contracts.Setup;

namespace BillMitra.Application.Common.Interfaces;

public interface ISetupService
{
    Task<SetupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<SetupStatusDto> CompleteSetupAsync(InitialSetupRequest request, CancellationToken cancellationToken = default);
}

public interface ICompanyService
{
    Task<CompanyProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<CompanyProfileDto> UpdateProfileAsync(UpdateCompanyProfileRequest request, CancellationToken cancellationToken = default);
}

public interface IFinancialYearService
{
    Task<FinancialYearDto?> GetActiveFinancialYearAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialYearDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
