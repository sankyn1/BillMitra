using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Company;
using BillMitra.Contracts.FinancialYear;
using BillMitra.Contracts.Setup;
using BillMitra.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class SetupService : ISetupService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IFinancialYearRepository _financialYearRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SetupService> _logger;

    public SetupService(
        ICompanyRepository companyRepository,
        IFinancialYearRepository financialYearRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<SetupService> logger)
    {
        _companyRepository = companyRepository;
        _financialYearRepository = financialYearRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<SetupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var activeFy = await _financialYearRepository.GetActiveFinancialYearAsync(cancellationToken);
        var hasUsers = await _userRepository.HasUsersAsync(cancellationToken);

        bool isCompleted = company != null && hasUsers;

        return new SetupStatusDto
        {
            IsCompleted = isCompleted,
            CompanyName = company?.LegalName,
            TradeName = company?.TradeName,
            Gstin = company?.Gstin,
            StateCode = company?.StateCode,
            StateName = company?.StateName,
            CurrentFinancialYear = activeFy?.Code,
            InitializedAtUtc = company?.CreatedAtUtc
        };
    }

    public async Task<SetupStatusDto> CompleteSetupAsync(InitialSetupRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LegalName))
            throw new ArgumentException("Legal Name of Business is required.", nameof(request.LegalName));

        if (string.IsNullOrWhiteSpace(request.StateCode))
            throw new ArgumentException("State Code is required.", nameof(request.StateCode));

        if (!string.IsNullOrWhiteSpace(request.Gstin))
        {
            if (!CompanyProfile.IsValidGstin(request.Gstin))
                throw new ArgumentException("Invalid GSTIN format. Expected 15-character alphanumeric format.", nameof(request.Gstin));

            var gstinState = CompanyProfile.ExtractStateCodeFromGstin(request.Gstin);
            if (!string.Equals(gstinState, request.StateCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"State code '{request.StateCode}' does not match GSTIN state prefix '{gstinState}'.", nameof(request.StateCode));
            }
        }

        if (string.IsNullOrWhiteSpace(request.OwnerUsername))
            throw new ArgumentException("Administrator username is required.", nameof(request.OwnerUsername));

        if (string.IsNullOrWhiteSpace(request.OwnerPassword) || request.OwnerPassword.Length < 6)
            throw new ArgumentException("Administrator password must be at least 6 characters.", nameof(request.OwnerPassword));

        _logger.LogInformation("Completing First-Run setup for {BusinessName}...", request.LegalName);

        // 1. Create or Update Company Profile
        var existingCompany = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        var company = existingCompany ?? new CompanyProfile();

        company.LegalName = request.LegalName.Trim();
        company.TradeName = string.IsNullOrWhiteSpace(request.TradeName) ? request.LegalName.Trim() : request.TradeName.Trim();
        company.Gstin = string.IsNullOrWhiteSpace(request.Gstin) ? null : request.Gstin.Trim().ToUpperInvariant();
        company.Pan = company.Gstin != null ? CompanyProfile.ExtractPanFromGstin(company.Gstin) : null;
        company.GstRegistrationType = request.GstRegistrationType;
        company.StateCode = request.StateCode.Trim();
        company.StateName = string.IsNullOrWhiteSpace(request.StateName) ? $"State {request.StateCode}" : request.StateName.Trim();
        company.AddressLine1 = request.AddressLine1?.Trim() ?? string.Empty;
        company.AddressLine2 = request.AddressLine2?.Trim();
        company.City = request.City?.Trim() ?? string.Empty;
        company.Pincode = request.Pincode?.Trim() ?? string.Empty;
        company.Phone = request.Phone?.Trim() ?? string.Empty;
        company.Email = request.Email?.Trim();
        company.BankName = request.BankName?.Trim();
        company.BankAccountNumber = request.BankAccountNumber?.Trim();
        company.BankIfsc = request.BankIfsc?.Trim();
        company.BankBranch = request.BankBranch?.Trim();
        company.UpiId = request.UpiId?.Trim();
        company.UpdatedAtUtc = DateTime.UtcNow;

        if (existingCompany == null)
        {
            company.Id = Guid.NewGuid();
            company.CreatedAtUtc = DateTime.UtcNow;
            await _companyRepository.CreateAsync(company, cancellationToken);
        }
        else
        {
            await _companyRepository.UpdateAsync(company, cancellationToken);
        }

        // 2. Initialize Financial Year
        var activeFy = await _financialYearRepository.GetActiveFinancialYearAsync(cancellationToken);
        if (activeFy == null)
        {
            int startYear = request.FinancialYearStartYear > 2000 ? request.FinancialYearStartYear : DateTime.Today.Month < 4 ? DateTime.Today.Year - 1 : DateTime.Today.Year;
            var newFy = FinancialYear.CreateDefaultIndianFinancialYear(startYear);
            activeFy = await _financialYearRepository.CreateAsync(newFy, cancellationToken);
        }

        // 3. Initialize Administrator User
        var existingUser = await _userRepository.GetByUsernameAsync(request.OwnerUsername.Trim(), cancellationToken);
        if (existingUser == null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.OwnerUsername.Trim(),
                FullName = string.IsNullOrWhiteSpace(request.OwnerFullName) ? request.OwnerUsername.Trim() : request.OwnerFullName.Trim(),
                Email = request.OwnerEmail?.Trim() ?? string.Empty,
                PasswordHash = _passwordHasher.HashPassword(request.OwnerPassword),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _userRepository.CreateAsync(user, new[] { "Owner", "Admin" }, cancellationToken);
        }

        return await GetStatusAsync(cancellationToken);
    }
}

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;

    public CompanyService(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<CompanyProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (entity == null) return null;

        return MapToDto(entity);
    }

    public async Task<CompanyProfileDto> UpdateProfileAsync(UpdateCompanyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (entity == null)
        {
            entity = new CompanyProfile { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
        }

        entity.LegalName = request.LegalName;
        entity.TradeName = request.TradeName;
        entity.Gstin = request.Gstin;
        entity.Pan = !string.IsNullOrWhiteSpace(request.Gstin) ? CompanyProfile.ExtractPanFromGstin(request.Gstin) : null;
        entity.GstRegistrationType = request.GstRegistrationType;
        entity.StateCode = request.StateCode;
        entity.StateName = request.StateName;
        entity.AddressLine1 = request.AddressLine1;
        entity.AddressLine2 = request.AddressLine2;
        entity.City = request.City;
        entity.Pincode = request.Pincode;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.BankName = request.BankName;
        entity.BankAccountNumber = request.BankAccountNumber;
        entity.BankIfsc = request.BankIfsc;
        entity.BankBranch = request.BankBranch;
        entity.UpiId = request.UpiId;
        entity.InvoiceFooterNotes = request.InvoiceFooterNotes;
        entity.TermsAndConditions = request.TermsAndConditions;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.CreatedAtUtc == default)
        {
            entity.CreatedAtUtc = DateTime.UtcNow;
            await _companyRepository.CreateAsync(entity, cancellationToken);
        }
        else
        {
            await _companyRepository.UpdateAsync(entity, cancellationToken);
        }

        return MapToDto(entity);
    }

    private static CompanyProfileDto MapToDto(CompanyProfile e) => new()
    {
        Id = e.Id,
        LegalName = e.LegalName,
        TradeName = e.TradeName,
        Gstin = e.Gstin,
        Pan = e.Pan,
        GstRegistrationType = e.GstRegistrationType,
        StateCode = e.StateCode,
        StateName = e.StateName,
        AddressLine1 = e.AddressLine1,
        AddressLine2 = e.AddressLine2,
        City = e.City,
        Pincode = e.Pincode,
        Phone = e.Phone,
        Email = e.Email,
        BankName = e.BankName,
        BankAccountNumber = e.BankAccountNumber,
        BankIfsc = e.BankIfsc,
        BankBranch = e.BankBranch,
        UpiId = e.UpiId,
        InvoiceFooterNotes = e.InvoiceFooterNotes,
        TermsAndConditions = e.TermsAndConditions,
        CreatedAtUtc = e.CreatedAtUtc,
        UpdatedAtUtc = e.UpdatedAtUtc
    };
}

public class FinancialYearService : IFinancialYearService
{
    private readonly IFinancialYearRepository _financialYearRepository;

    public FinancialYearService(IFinancialYearRepository financialYearRepository)
    {
        _financialYearRepository = financialYearRepository;
    }

    public async Task<FinancialYearDto?> GetActiveFinancialYearAsync(CancellationToken cancellationToken = default)
    {
        var fy = await _financialYearRepository.GetActiveFinancialYearAsync(cancellationToken);
        if (fy == null) return null;

        return new FinancialYearDto
        {
            Id = fy.Id,
            Code = fy.Code,
            StartDate = fy.StartDate,
            EndDate = fy.EndDate,
            IsActive = fy.IsCurrent,
            IsLocked = fy.IsClosed,
            CreatedAtUtc = fy.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<FinancialYearDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _financialYearRepository.GetAllAsync(cancellationToken);
        return list.Select(fy => new FinancialYearDto
        {
            Id = fy.Id,
            Code = fy.Code,
            StartDate = fy.StartDate,
            EndDate = fy.EndDate,
            IsActive = fy.IsCurrent,
            IsLocked = fy.IsClosed,
            CreatedAtUtc = fy.CreatedAtUtc
        }).ToList();
    }
}
