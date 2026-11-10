namespace BillMitra.Contracts.FinancialYear;

public class FinancialYearDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "2026-2027"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

