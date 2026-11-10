namespace BillMitra.Domain.Entities;

public class FinancialYear
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // e.g. "2026-27"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public static FinancialYear CreateDefaultIndianFinancialYear(int startYear = 2026)
    {
        var endYearShort = (startYear + 1) % 100;
        return new FinancialYear
        {
            Code = $"{startYear}-{endYearShort:D2}",
            StartDate = new DateOnly(startYear, 4, 1),
            EndDate = new DateOnly(startYear + 1, 3, 31),
            IsCurrent = true,
            IsClosed = false
        };
    }
}
