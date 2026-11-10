using System;
using BillMitra.Domain.Enums;

namespace BillMitra.Domain.Entities;

public class GstReturnFiling
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public GstReturnType ReturnType { get; set; } = GstReturnType.Gstr1;
    public string FinancialYear { get; set; } = string.Empty;
    public string TaxPeriod { get; set; } = string.Empty; // e.g. "042026" (MMYYYY)
    public GstFilingStatus FilingStatus { get; set; } = GstFilingStatus.NotPrepared;
    public string? Arn { get; set; }
    public DateTime? ArnDate { get; set; }
    public string? ValidationErrorsJson { get; set; }
    public string? PayloadJson { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderReference { get; set; }
    public string? ProviderResponseJson { get; set; }
    public decimal TotalTaxableValue { get; set; }
    public decimal TotalTaxLiability { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FiledAtUtc { get; set; }

    public bool CanSubmit() =>
        FilingStatus == GstFilingStatus.Prepared ||
        FilingStatus == GstFilingStatus.ReadyForSubmission ||
        FilingStatus == GstFilingStatus.Rejected;
}
