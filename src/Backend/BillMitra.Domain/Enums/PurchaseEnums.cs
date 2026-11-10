namespace BillMitra.Domain.Enums;

public enum PurchaseDocumentType
{
    PurchaseOrder = 1,
    PurchaseInvoice = 2,
    PurchaseReturn = 3
}

public enum PurchaseDocumentStatus
{
    Draft = 1,
    Finalized = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5
}

public enum ItcEligibility
{
    EligibleOther = 1,          // Inputs & Input Services (Table 4A(5))
    EligibleCapitalGoods = 2,   // Capital Goods (Table 4A(4))
    IneligibleSection17_5 = 3,  // Ineligible under Section 17(5) (Table 4D(1))
    InwardReverseCharge = 4     // Inward RCM liability with eligible ITC (Table 4A(3))
}
