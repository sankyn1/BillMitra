using System;
using System.Collections.Generic;

namespace BillMitra.Contracts.Reports;

public record FinancialOverviewReportDto(
    decimal TotalSales,
    decimal TotalPurchases,
    decimal TotalExpenses,
    decimal GrossProfit,
    decimal NetProfit,
    decimal ProfitMarginPercent,
    decimal TotalReceivables,
    decimal TotalPayables,
    decimal TotalStockValuation,
    DateTime FromDate,
    DateTime ToDate
);

public record MonthlyFinancialTrendDto(
    string MonthLabel,
    int Year,
    int Month,
    decimal SalesAmount,
    decimal PurchaseAmount,
    decimal ExpenseAmount,
    decimal NetProfit
);

public record GstLiabilitySummaryDto(
    decimal TotalOutwardTaxable,
    decimal OutwardCgst,
    decimal OutwardSgst,
    decimal OutwardIgst,
    decimal TotalOutwardTax,
    decimal TotalInwardTaxable,
    decimal InwardEligibleCgst,
    decimal InwardEligibleSgst,
    decimal InwardEligibleIgst,
    decimal TotalEligibleItc,
    decimal NetPayableCgst,
    decimal NetPayableSgst,
    decimal NetPayableIgst,
    decimal TotalNetGstPayable
);

public record AgingBucketDto(
    string BucketName, // 0-30 Days, 31-60 Days, 61-90 Days, 90+ Days
    decimal TotalReceivableAmount,
    int InvoicesCount,
    decimal TotalPayableAmount,
    int BillsCount
);

public record PartyAgingDetailDto(
    Guid PartyId,
    string PartyName,
    string? Gstin,
    bool IsCustomer,
    decimal CurrentBalance,
    decimal Days0To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus
);

public record ProductMovementVelocityDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal TotalSoldQuantity,
    decimal TotalRevenue,
    decimal CurrentStock,
    string VelocityClassification // Fast Moving, Moderate, Slow Moving / Dead Stock
);

public record ComprehensiveReportsBundleDto(
    FinancialOverviewReportDto Overview,
    List<MonthlyFinancialTrendDto> MonthlyTrends,
    GstLiabilitySummaryDto GstSummary,
    List<AgingBucketDto> AgingBuckets,
    List<PartyAgingDetailDto> TopOverdueParties,
    List<ProductMovementVelocityDto> ProductVelocities
);

