namespace BillMitra.Contracts.Setup;

public class SetupStatusDto
{
    public bool IsCompleted { get; set; }
    public string? CompanyName { get; set; }
    public string? TradeName { get; set; }
    public string? Gstin { get; set; }
    public string? StateCode { get; set; }
    public string? StateName { get; set; }
    public string? CurrentFinancialYear { get; set; }
    public DateTime? InitializedAtUtc { get; set; }
}

public class InitialSetupRequest
{
    // Business Identity
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public int GstRegistrationType { get; set; } = 1; // 1 = Regular, 2 = Composition, etc.
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;

    // Address & Contact
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    // Banking Details (Optional)
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfsc { get; set; }
    public string? BankBranch { get; set; }
    public string? UpiId { get; set; }

    // Financial Year
    public int FinancialYearStartYear { get; set; } = 2026;

    // Administrator Account
    public string OwnerFullName { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
}

