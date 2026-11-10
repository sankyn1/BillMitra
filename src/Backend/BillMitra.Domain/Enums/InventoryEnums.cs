namespace BillMitra.Domain.Enums;

public enum StockMovementType
{
    PurchaseInward = 1,
    SalesOutward = 2,
    PurchaseReturn = 3,
    SalesReturn = 4,
    AdjustmentAddition = 5,
    AdjustmentReduction = 6,
    TransferIn = 7,
    TransferOut = 8
}

public enum StockAdjustmentReason
{
    PhysicalCountReconciliation = 1,
    DamagedOrBroken = 2,
    ExpiredGoods = 3,
    WastageOrTheft = 4,
    InitialStockTake = 5,
    Other = 6
}

public enum StockValuationMethod
{
    WeightedAverage = 1,
    FirstInFirstOut = 2
}

