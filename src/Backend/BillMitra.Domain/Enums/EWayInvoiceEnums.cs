namespace BillMitra.Domain.Enums;

public enum EWayBillStatus
{
    Generated = 1,
    Updated = 2,
    Cancelled = 3,
    Expired = 4
}

public enum TransportationMode
{
    Road = 1,
    Rail = 2,
    Air = 3,
    Ship = 4
}

public enum VehicleType
{
    Regular = 1,
    OverDimensionalCargo = 2
}

public enum EWayBillSubSupplyType
{
    Supply = 1,
    Import = 2,
    Export = 3,
    JobWork = 4,
    ForOwnUse = 5,
    JobWorkReturns = 6,
    SalesReturn = 7,
    Others = 8
}

public enum EInvoiceStatus
{
    Generated = 1,
    Cancelled = 2,
    Failed = 3
}

public enum EInvoiceCancelReason
{
    Duplicate = 1,
    DataEntryMistake = 2,
    OrderCancelled = 3,
    Other = 4
}
