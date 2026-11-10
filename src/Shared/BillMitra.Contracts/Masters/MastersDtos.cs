namespace BillMitra.Contracts.Masters;

public class UnitDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "PCS", "KGS", "BOX", "MTR", "NOS", "LTR", "BAG", "SET"
    public string Name { get; set; } = string.Empty; // e.g. "Pieces", "Kilograms", "Boxes"
    public bool IsActive { get; set; } = true;
}

public class ProductCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string ItemCode { get; set; } = string.Empty; // SKU or Barcode
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ItemType { get; set; } = 1; // 1 = Goods, 2 = Service
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string HsnSacCode { get; set; } = string.Empty; // 4, 6 or 8 digits
    public string UnitCode { get; set; } = "PCS"; // Default UOM
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal GstRatePercent { get; set; } // 0, 0.1, 0.25, 1.5, 3, 5, 12, 18, 28
    public decimal CessPercent { get; set; } = 0;
    public bool IsTaxInclusive { get; set; } = false;
    public decimal OpeningStock { get; set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinStockAlert { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateOrUpdateProductRequest
{
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ItemType { get; set; } = 1; // 1 = Goods, 2 = Service
    public Guid? CategoryId { get; set; }
    public string HsnSacCode { get; set; } = string.Empty;
    public string UnitCode { get; set; } = "PCS";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal GstRatePercent { get; set; } = 18.0m;
    public decimal CessPercent { get; set; } = 0;
    public bool IsTaxInclusive { get; set; } = false;
    public decimal OpeningStock { get; set; } = 0;
    public decimal MinStockAlert { get; set; } = 0;
}

public class PartyDto
{
    public Guid Id { get; set; }
    public int PartyType { get; set; } = 1; // 1 = Customer, 2 = Supplier, 3 = Both
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public int GstRegistrationType { get; set; } = 1; // 1 = Regular, 2 = Composition, 3 = Unregistered, 4 = Consumer
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal CreditLimit { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0; // Negative = Payable, Positive = Receivable
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateOrUpdatePartyRequest
{
    public int PartyType { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
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
    public decimal CreditLimit { get; set; } = 0;
}

public class ProductCsvImportRow
{
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HsnSacCode { get; set; } = string.Empty;
    public string UnitCode { get; set; } = "PCS";
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal GstRatePercent { get; set; }
    public decimal OpeningStock { get; set; }
}
