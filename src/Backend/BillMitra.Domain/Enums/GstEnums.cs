namespace BillMitra.Domain.Enums;

public enum GstRegistrationType
{
    Regular = 1,
    Composition = 2,
    Unregistered = 3,
    Consumer = 4,
    Overseas = 5,
    SpecialEconomicZone = 6,
    DeemedExport = 7
}

public enum DocumentType
{
    TaxInvoice = 1,
    BillOfSupply = 2,
    Quotation = 3,
    ProformaInvoice = 4,
    CreditNote = 5,
    DebitNote = 6,
    DeliveryChallan = 7,
    ReceiptVoucher = 8
}

public enum DocumentStatus
{
    Draft = 1,
    Finalized = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
    Converted = 6
}

public enum SupplyType
{
    IntraState = 1,
    InterState = 2,
    Export = 3
}

public enum PaymentMode
{
    Cash = 1,
    BankTransfer = 2,
    Cheque = 3,
    UPI = 4,
    CreditCard = 5,
    DebitCard = 6
}

public enum PaymentType
{
    Receipt = 1,
    Payment = 2
}

public enum PaymentStatus
{
    Active = 1,
    Reversed = 2
}

public enum GstReturnType
{
    Gstr1 = 1,
    Gstr3b = 2
}

public enum GstFilingStatus
{
    NotPrepared = 0,
    Prepared = 1,
    ValidationFailed = 2,
    ReadyForSubmission = 3,
    SubmittedToProvider = 4,
    Pending = 5,
    Accepted = 6,
    Rejected = 7,
    Filed = 8
}

public enum GstSupplyCategory
{
    B2B = 1,
    B2CL = 2,
    B2CS = 3,
    CDNR = 4,
    CDNUR = 5,
    Export = 6,
    NilExempt = 7
}


