using System.Text.RegularExpressions;

namespace BillMitra.Domain.Entities;

public class CompanyProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public int GstRegistrationType { get; set; } = 1; // 1 = Regular, 2 = Composition, etc.
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfsc { get; set; }
    public string? BankBranch { get; set; }
    public string? UpiId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    private static readonly Regex GstinRegex = new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", RegexOptions.Compiled);

    public static bool IsValidGstin(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin)) return false;
        return GstinRegex.IsMatch(gstin.Trim().ToUpperInvariant());
    }

    public static string? ExtractPanFromGstin(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin) || gstin.Trim().Length < 12) return null;
        var clean = gstin.Trim().ToUpperInvariant();
        return clean.Length >= 12 ? clean.Substring(2, 10) : null;
    }

    public static string? ExtractStateCodeFromGstin(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin) || gstin.Trim().Length < 2) return null;
        return gstin.Trim().Substring(0, 2);
    }
}
