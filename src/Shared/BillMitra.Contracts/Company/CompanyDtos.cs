namespace BillMitra.Contracts.Company;

public class CompanyProfileDto
{
    public Guid Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public int GstRegistrationType { get; set; } = 1;
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfsc { get; set; }
    public string? BankBranch { get; set; }
    public string? UpiId { get; set; }
    public string? InvoiceFooterNotes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? LetterheadImagePath { get; set; }
    public int DefaultLetterheadMode { get; set; } = 1; // 1 = WithLetterhead, 2 = PreprintedStationery
    public int PreprintedHeaderMarginMm { get; set; } = 50;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class UpdateCompanyProfileRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public int GstRegistrationType { get; set; } = 1;
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfsc { get; set; }
    public string? BankBranch { get; set; }
    public string? UpiId { get; set; }
    public string? InvoiceFooterNotes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? LetterheadImagePath { get; set; }
    public int DefaultLetterheadMode { get; set; } = 1;
    public int PreprintedHeaderMarginMm { get; set; } = 50;
}

public record UpdateLetterheadSettingsRequest(
    int DefaultLetterheadMode,
    int PreprintedHeaderMarginMm
);

public record UploadLetterheadRequest(
    string Base64Data,
    string? FileName = null
);

