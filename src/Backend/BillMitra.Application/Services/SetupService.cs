using System.Data;
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
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<SetupService> _logger;

    public SetupService(
        ICompanyRepository companyRepository,
        IFinancialYearRepository financialYearRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IDbConnectionFactory dbConnectionFactory,
        ILogger<SetupService> logger)
    {
        _companyRepository = companyRepository;
        _financialYearRepository = financialYearRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _dbConnectionFactory = dbConnectionFactory;
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

    public async Task<bool> ResetTransactionalAndMasterDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Resetting all transactional and master data, resetting document sequences to 0...");

        using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();

        using var tx = conn.BeginTransaction();
        try
        {
            var statements = new[]
            {
                "DELETE FROM sales_document_items",
                "DELETE FROM sales_documents",
                "DELETE FROM purchase_document_items",
                "DELETE FROM purchase_documents",
                "DELETE FROM expense_vouchers",
                "DELETE FROM expenses",
                "DELETE FROM payment_allocations",
                "DELETE FROM payments",
                "DELETE FROM party_ledger_entries",
                "DELETE FROM eway_bills",
                "DELETE FROM einvoices",
                "DELETE FROM stock_movements",
                "DELETE FROM stock_item_batches",
                "DELETE FROM products",
                "DELETE FROM parties",
                "DELETE FROM gst_return_filings",
                "UPDATE document_sequences SET current_sequence = 0"
            };

            foreach (var stmt in statements)
            {
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = stmt;
                    await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Statement '{Statement}' notice: {Error}", stmt, ex.Message);
                }
            }

            tx.Commit();
            _logger.LogInformation("All transactional and master data cleared and sequences reset successfully.");
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to reset transactional and master data.");
            throw;
        }
    }

    public async Task<bool> ResetSelectiveDataAsync(SelectiveResetRequest request, CancellationToken cancellationToken = default)
    {
        // Verify Password
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Secret reset password is required.");
        }

        // Check against master override secret or active user password
        bool isAuthorized = request.Password == "BillMitraReset@2026";
        if (!isAuthorized)
        {
            var adminUser = await _userRepository.GetByUsernameAsync("admin", cancellationToken);
            if (adminUser != null && _passwordHasher.VerifyPassword(request.Password, adminUser.PasswordHash))
            {
                isAuthorized = true;
            }
        }

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException("Invalid secret reset password.");
        }

        _logger.LogWarning("Performing selective reset based on user choices...");

        using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        if (conn.State != ConnectionState.Open) conn.Open();

        using var tx = conn.BeginTransaction();
        try
        {
            var stmts = new List<string>();

            if (request.ResetSalesInvoices)
            {
                stmts.Add("DELETE FROM sales_document_items");
                stmts.Add("DELETE FROM sales_documents");
            }

            if (request.ResetPurchases)
            {
                stmts.Add("DELETE FROM purchase_document_items");
                stmts.Add("DELETE FROM purchase_documents");
            }

            if (request.ResetExpenses)
            {
                stmts.Add("DELETE FROM expense_vouchers");
                stmts.Add("DELETE FROM expenses");
            }

            if (request.ResetPaymentsAndLedgers)
            {
                stmts.Add("DELETE FROM payment_allocations");
                stmts.Add("DELETE FROM payments");
                stmts.Add("DELETE FROM party_ledger_entries");
            }

            if (request.ResetEWayAndEInvoices)
            {
                stmts.Add("DELETE FROM eway_bills");
                stmts.Add("DELETE FROM einvoices");
            }

            if (request.ResetStockAndBatches)
            {
                stmts.Add("DELETE FROM stock_movements");
                stmts.Add("DELETE FROM stock_item_batches");
            }

            if (request.ResetGstFilings)
            {
                stmts.Add("DELETE FROM gst_return_filings");
            }

            if (request.ResetProducts)
            {
                stmts.Add("DELETE FROM stock_movements");
                stmts.Add("DELETE FROM stock_item_batches");
                stmts.Add("DELETE FROM sales_document_items");
                stmts.Add("DELETE FROM purchase_document_items");
                stmts.Add("DELETE FROM products");
            }

            if (request.ResetParties)
            {
                stmts.Add("DELETE FROM party_ledger_entries");
                stmts.Add("DELETE FROM payment_allocations");
                stmts.Add("DELETE FROM payments");
                stmts.Add("DELETE FROM sales_documents");
                stmts.Add("DELETE FROM purchase_documents");
                stmts.Add("DELETE FROM parties");
            }

            if (request.ResetDocumentSequences)
            {
                stmts.Add("UPDATE document_sequences SET current_sequence = 0");
            }

            foreach (var stmt in stmts.Distinct())
            {
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = stmt;
                    await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Statement '{Statement}' notice: {Error}", stmt, ex.Message);
                }
            }

            tx.Commit();
            _logger.LogInformation("Selective database reset completed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to perform selective database reset.");
            throw;
        }
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
        entity.LetterheadImagePath = request.LetterheadImagePath;
        entity.DefaultLetterheadMode = request.DefaultLetterheadMode > 0 ? request.DefaultLetterheadMode : 1;
        entity.PreprintedHeaderMarginMm = request.PreprintedHeaderMarginMm > 0 ? request.PreprintedHeaderMarginMm : 50;
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

    public async Task<CompanyProfileDto> UpdateLetterheadSettingsAsync(UpdateLetterheadSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (entity == null)
            throw new InvalidOperationException("Company profile does not exist.");

        entity.DefaultLetterheadMode = request.DefaultLetterheadMode > 0 ? request.DefaultLetterheadMode : 1;
        entity.PreprintedHeaderMarginMm = request.PreprintedHeaderMarginMm > 0 ? request.PreprintedHeaderMarginMm : 50;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    public async Task<string> SaveLetterheadImageAsync(string base64Data, string? fileName, CancellationToken cancellationToken = default)
    {
        var entity = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (entity == null)
            throw new InvalidOperationException("Company profile does not exist.");

        // Clean base64 header if present (e.g. data:image/png;base64,...)
        var cleanBase64 = base64Data;
        if (cleanBase64.Contains(','))
        {
            cleanBase64 = cleanBase64.Substring(cleanBase64.IndexOf(',') + 1);
        }

        var imageBytes = Convert.FromBase64String(cleanBase64);

        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "letterheads");
        Directory.CreateDirectory(dataDir);

        var ext = ".png";
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var detectedExt = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(detectedExt)) ext = detectedExt;
        }

        var filePath = Path.Combine(dataDir, $"letterhead_{entity.Id:N}{ext}");
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        entity.LetterheadImagePath = filePath;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _companyRepository.UpdateAsync(entity, cancellationToken);

        return filePath;
    }

    public async Task<bool> RemoveLetterheadImageAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _companyRepository.GetPrimaryCompanyAsync(cancellationToken);
        if (entity == null) return false;

        if (!string.IsNullOrWhiteSpace(entity.LetterheadImagePath) && File.Exists(entity.LetterheadImagePath))
        {
            try { File.Delete(entity.LetterheadImagePath); } catch {}
        }

        entity.LetterheadImagePath = null;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _companyRepository.UpdateAsync(entity, cancellationToken);

        return true;
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
        LetterheadImagePath = e.LetterheadImagePath,
        DefaultLetterheadMode = e.DefaultLetterheadMode,
        PreprintedHeaderMarginMm = e.PreprintedHeaderMarginMm,
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
