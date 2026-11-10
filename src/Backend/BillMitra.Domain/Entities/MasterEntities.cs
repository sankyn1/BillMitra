namespace BillMitra.Domain.Entities;

public class UnitOfMeasure
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // e.g. "PCS", "KGS"
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ProductCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ItemCode { get; set; } = string.Empty; // Barcode / SKU
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ItemType { get; set; } = 1; // 1 = Goods, 2 = Service
    public Guid? CategoryId { get; set; }
    public string HsnSacCode { get; set; } = string.Empty; // 4, 6 or 8 digits
    public string UnitCode { get; set; } = "PCS";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal GstRatePercent { get; set; } = 18.0m;
    public decimal CessPercent { get; set; } = 0;
    public bool IsTaxInclusive { get; set; } = false;
    public decimal OpeningStock { get; set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinStockAlert { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public static readonly decimal[] ValidGstRates = [0m, 0.1m, 0.25m, 1.5m, 3m, 5m, 12m, 18m, 28m];

    public static bool IsValidHsn(string? hsn)
    {
        if (string.IsNullOrWhiteSpace(hsn)) return false;
        var clean = hsn.Trim();
        return (clean.Length == 2 || clean.Length == 4 || clean.Length == 6 || clean.Length == 8) 
            && clean.All(char.IsDigit);
    }
}

public class Party
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PartyType { get; set; } = 1; // 1 = Customer, 2 = Supplier, 3 = Both
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
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
    public decimal CreditLimit { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
