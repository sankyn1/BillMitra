using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Reports;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReportRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FinancialOverviewReportDto> GetFinancialOverviewAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();

        // 1. Total Sales (Finalized/Paid tax invoices)
        var salesSql = @"
            SELECT COALESCE(SUM(grand_total), 0)
            FROM sales_documents
            WHERE document_status IN (2, 3, 4)
              AND document_date >= @FromDate AND document_date <= @ToDate";
        var totalSales = await conn.ExecuteScalarAsync<decimal>(salesSql, new { FromDate = fromDate, ToDate = toDate });

        // 2. Total Purchases (Finalized/Paid inward bills)
        var purchaseSql = @"
            SELECT COALESCE(SUM(grand_total), 0)
            FROM purchase_documents
            WHERE document_status IN (2, 3, 4)
              AND document_date >= @FromDate AND document_date <= @ToDate";
        var totalPurchases = await conn.ExecuteScalarAsync<decimal>(purchaseSql, new { FromDate = fromDate, ToDate = toDate });

        // 3. Total Expenses
        var expenseSql = @"
            SELECT COALESCE(SUM(amount + gst_amount), 0)
            FROM expense_entries
            WHERE expense_date >= @FromDate AND expense_date <= @ToDate";
        var totalExpenses = await conn.ExecuteScalarAsync<decimal>(expenseSql, new { FromDate = fromDate, ToDate = toDate });

        // 4. Receivables (Unpaid sales balance)
        var receivablesSql = @"
            SELECT COALESCE(SUM(grand_total - paid_amount), 0)
            FROM sales_documents
            WHERE document_status IN (2, 4)";
        var totalReceivables = await conn.ExecuteScalarAsync<decimal>(receivablesSql);

        // 5. Payables (Unpaid inward balance)
        var payablesSql = @"
            SELECT COALESCE(SUM(grand_total - paid_amount), 0)
            FROM purchase_documents
            WHERE document_status IN (2, 4)";
        var totalPayables = await conn.ExecuteScalarAsync<decimal>(payablesSql);

        // 6. Total Stock Valuation
        var stockSql = @"
            SELECT COALESCE(SUM(p.purchase_price * COALESCE(sm.total_qty, 0)), 0)
            FROM products p
            LEFT JOIN (
                SELECT product_id, SUM(quantity) as total_qty
                FROM stock_movements
                GROUP BY product_id
            ) sm ON p.id = sm.product_id
            WHERE p.is_active = 1";
        var totalStockValuation = await conn.ExecuteScalarAsync<decimal>(stockSql);

        var grossProfit = totalSales - totalPurchases;
        var netProfit = grossProfit - totalExpenses;
        var profitMargin = totalSales > 0 ? Math.Round((netProfit / totalSales) * 100m, 2) : 0m;

        return new FinancialOverviewReportDto(
            TotalSales: totalSales,
            TotalPurchases: totalPurchases,
            TotalExpenses: totalExpenses,
            GrossProfit: grossProfit,
            NetProfit: netProfit,
            ProfitMarginPercent: profitMargin,
            TotalReceivables: totalReceivables,
            TotalPayables: totalPayables,
            TotalStockValuation: totalStockValuation,
            FromDate: fromDate,
            ToDate: toDate
        );
    }

    public async Task<IReadOnlyList<MonthlyFinancialTrendDto>> GetMonthlyFinancialTrendsAsync(Guid companyId, int numberOfMonths, CancellationToken cancellationToken = default)
    {
        var result = new List<MonthlyFinancialTrendDto>();
        var current = DateTime.UtcNow.Date;

        for (int i = numberOfMonths - 1; i >= 0; i--)
        {
            var targetMonth = current.AddMonths(-i);
            var startOfMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var overview = await GetFinancialOverviewAsync(companyId, startOfMonth, endOfMonth, cancellationToken);
            var label = startOfMonth.ToString("MMM yyyy");

            result.Add(new MonthlyFinancialTrendDto(
                MonthLabel: label,
                Year: targetMonth.Year,
                Month: targetMonth.Month,
                SalesAmount: overview.TotalSales,
                PurchaseAmount: overview.TotalPurchases,
                ExpenseAmount: overview.TotalExpenses,
                NetProfit: overview.NetProfit
            ));
        }

        return result;
    }

    public async Task<GstLiabilitySummaryDto> GetGstLiabilitySummaryAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();

        // Outward Sales Taxes
        var outwardSql = @"
            SELECT 
                COALESCE(SUM(sub_total_taxable), 0) as Taxable,
                COALESCE(SUM(cgst_total), 0) as Cgst,
                COALESCE(SUM(sgst_total), 0) as Sgst,
                COALESCE(SUM(igst_total), 0) as Igst
            FROM sales_documents
            WHERE document_status IN (2, 3, 4)
              AND document_date >= @FromDate AND document_date <= @ToDate";

        var outward = await conn.QuerySingleOrDefaultAsync(outwardSql, new { FromDate = fromDate, ToDate = toDate });

        decimal outwardTaxable = outward?.Taxable != null ? Convert.ToDecimal(outward.Taxable) : 0m;
        decimal outwardCgst = outward?.Cgst != null ? Convert.ToDecimal(outward.Cgst) : 0m;
        decimal outwardSgst = outward?.Sgst != null ? Convert.ToDecimal(outward.Sgst) : 0m;
        decimal outwardIgst = outward?.Igst != null ? Convert.ToDecimal(outward.Igst) : 0m;
        var totalOutwardTax = outwardCgst + outwardSgst + outwardIgst;

        // Inward Purchases Taxes (Eligible ITC)
        var inwardSql = @"
            SELECT 
                COALESCE(SUM(sub_total_taxable), 0) as Taxable,
                COALESCE(SUM(cgst_total), 0) as Cgst,
                COALESCE(SUM(sgst_total), 0) as Sgst,
                COALESCE(SUM(igst_total), 0) as Igst
            FROM purchase_documents
            WHERE document_status IN (2, 3, 4)
              AND default_itc_eligibility IN (1, 2)
              AND document_date >= @FromDate AND document_date <= @ToDate";

        var inward = await conn.QuerySingleOrDefaultAsync(inwardSql, new { FromDate = fromDate, ToDate = toDate });

        decimal inwardTaxable = inward?.Taxable != null ? Convert.ToDecimal(inward.Taxable) : 0m;
        decimal inwardCgst = inward?.Cgst != null ? Convert.ToDecimal(inward.Cgst) : 0m;
        decimal inwardSgst = inward?.Sgst != null ? Convert.ToDecimal(inward.Sgst) : 0m;
        decimal inwardIgst = inward?.Igst != null ? Convert.ToDecimal(inward.Igst) : 0m;
        var totalEligibleItc = inwardCgst + inwardSgst + inwardIgst;

        var netCgst = Math.Max(0, outwardCgst - inwardCgst);
        var netSgst = Math.Max(0, outwardSgst - inwardSgst);
        var netIgst = Math.Max(0, outwardIgst - inwardIgst);

        return new GstLiabilitySummaryDto(
            TotalOutwardTaxable: outwardTaxable,
            OutwardCgst: outwardCgst,
            OutwardSgst: outwardSgst,
            OutwardIgst: outwardIgst,
            TotalOutwardTax: totalOutwardTax,
            TotalInwardTaxable: inwardTaxable,
            InwardEligibleCgst: inwardCgst,
            InwardEligibleSgst: inwardSgst,
            InwardEligibleIgst: inwardIgst,
            TotalEligibleItc: totalEligibleItc,
            NetPayableCgst: netCgst,
            NetPayableSgst: netSgst,
            NetPayableIgst: netIgst,
            TotalNetGstPayable: netCgst + netSgst + netIgst
        );
    }

    public async Task<IReadOnlyList<AgingBucketDto>> GetAgingBucketsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var today = DateTime.UtcNow.Date;

        // Fetch unpaid sales documents
        var salesSql = @"
            SELECT id as Id, document_date as DocumentDate, (grand_total - paid_amount) as Balance
            FROM sales_documents
            WHERE document_status IN (2, 4) AND (grand_total - paid_amount) > 0";

        var unpaidSales = (await conn.QueryAsync<UnpaidDocRow>(salesSql)).ToList();

        // Fetch unpaid purchases
        var purchasesSql = @"
            SELECT id as Id, document_date as DocumentDate, (grand_total - paid_amount) as Balance
            FROM purchase_documents
            WHERE document_status IN (2, 4) AND (grand_total - paid_amount) > 0";

        var unpaidPurchases = (await conn.QueryAsync<UnpaidDocRow>(purchasesSql)).ToList();

        var bucketNames = new[] { "0-30 Days", "31-60 Days", "61-90 Days", "90+ Days" };
        var buckets = new List<AgingBucketDto>();

        foreach (var name in bucketNames)
        {
            var (min, max) = name switch
            {
                "0-30 Days" => (0, 30),
                "31-60 Days" => (31, 60),
                "61-90 Days" => (61, 90),
                _ => (91, 99999)
            };

            var matchingSales = unpaidSales.Where(s =>
            {
                var days = (today - s.DocumentDate.Date).TotalDays;
                return days >= min && days <= max;
            }).ToList();

            var matchingPurchases = unpaidPurchases.Where(p =>
            {
                var days = (today - p.DocumentDate.Date).TotalDays;
                return days >= min && days <= max;
            }).ToList();

            buckets.Add(new AgingBucketDto(
                BucketName: name,
                TotalReceivableAmount: matchingSales.Sum(s => s.Balance),
                InvoicesCount: matchingSales.Count,
                TotalPayableAmount: matchingPurchases.Sum(p => p.Balance),
                BillsCount: matchingPurchases.Count
            ));
        }

        return buckets;
    }

    public async Task<IReadOnlyList<PartyAgingDetailDto>> GetTopOverduePartiesAsync(Guid companyId, int topN = 10, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var today = DateTime.UtcNow.Date;

        var sql = @"
            SELECT 
                p.id as PartyId,
                p.name as PartyName,
                p.gstin as Gstin,
                (CASE WHEN p.party_type = 1 THEN 1 ELSE 0 END) as IsCustomer,
                s.document_date as DocumentDate,
                (s.grand_total - s.paid_amount) as Balance
            FROM parties p
            JOIN sales_documents s ON p.id = s.party_id
            WHERE s.document_status IN (2, 4) AND (s.grand_total - s.paid_amount) > 0";

        var records = (await conn.QueryAsync<PartyAgingRow>(sql)).ToList();
        var grouped = records.GroupBy(r => r.PartyId);

        var list = new List<PartyAgingDetailDto>();

        foreach (var group in grouped)
        {
            var first = group.First();
            decimal d0_30 = 0, d31_60 = 0, d61_90 = 0, d90plus = 0;
            decimal currentBal = 0;

            foreach (var doc in group)
            {
                var bal = doc.Balance;
                currentBal += bal;
                var days = (today - doc.DocumentDate.Date).TotalDays;

                if (days <= 30) d0_30 += bal;
                else if (days <= 60) d31_60 += bal;
                else if (days <= 90) d61_90 += bal;
                else d90plus += bal;
            }

            list.Add(new PartyAgingDetailDto(
                PartyId: first.PartyId,
                PartyName: first.PartyName,
                Gstin: first.Gstin,
                IsCustomer: first.IsCustomer == 1,
                CurrentBalance: currentBal,
                Days0To30: d0_30,
                Days31To60: d31_60,
                Days61To90: d61_90,
                Days90Plus: d90plus
            ));
        }

        return list.OrderByDescending(p => p.CurrentBalance).Take(topN).ToList();
    }

    public async Task<IReadOnlyList<ProductMovementVelocityDto>> GetProductMovementVelocitiesAsync(Guid companyId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                p.id as ProductId,
                p.name as ProductName,
                p.item_code as Sku,
                COALESCE(sales.sold_qty, 0) as SoldQty,
                COALESCE(sales.sold_val, 0) as SoldVal,
                COALESCE(sm.current_stock, 0) as CurrentStock
            FROM products p
            LEFT JOIN (
                SELECT product_id, SUM(quantity) as sold_qty, SUM(total_amount) as sold_val
                FROM sales_document_items
                GROUP BY product_id
            ) sales ON p.id = sales.product_id
            LEFT JOIN (
                SELECT product_id, SUM(quantity) as current_stock
                FROM stock_movements
                GROUP BY product_id
            ) sm ON p.id = sm.product_id
            WHERE p.is_active = 1
            ORDER BY SoldQty DESC";

        var rows = await conn.QueryAsync<ProductVelocityRow>(sql);
        var list = new List<ProductMovementVelocityDto>();

        foreach (var r in rows)
        {
            var soldQty = r.SoldQty;
            var soldVal = r.SoldVal;
            var currentStock = r.CurrentStock;

            string classification;
            if (soldQty >= 50) classification = "Fast Moving";
            else if (soldQty >= 10) classification = "Moderate";
            else classification = "Slow Moving / Dead Stock";

            list.Add(new ProductMovementVelocityDto(
                ProductId: r.ProductId,
                ProductName: r.ProductName,
                Sku: r.Sku ?? string.Empty,
                TotalSoldQuantity: soldQty,
                TotalRevenue: soldVal,
                CurrentStock: currentStock,
                VelocityClassification: classification
            ));
        }

        return list;
    }

    private sealed class UnpaidDocRow
    {
        public Guid Id { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal Balance { get; set; }
    }

    private sealed class PartyAgingRow
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string? Gstin { get; set; }
        public int IsCustomer { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal Balance { get; set; }
    }

    private sealed class ProductVelocityRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal SoldQty { get; set; }
        public decimal SoldVal { get; set; }
        public decimal CurrentStock { get; set; }
    }
}
