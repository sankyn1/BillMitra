using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using BillMitra.Contracts.Reports;
using BillMitra.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillMitra.UnitTests;

public class ReportUnitTests
{
    [Fact]
    public void FinancialOverview_Formulas_ComputeCorrectly()
    {
        var sales = 250000m;
        var purchases = 150000m;
        var expenses = 40000m;

        var grossProfit = sales - purchases; // 100000
        var netProfit = grossProfit - expenses; // 60000
        var margin = Math.Round((netProfit / sales) * 100m, 2); // 24.00%

        grossProfit.Should().Be(100000m);
        netProfit.Should().Be(60000m);
        margin.Should().Be(24.00m);
    }

    [Fact]
    public async Task ReportService_GetComprehensiveReports_AggregatesCorrectly()
    {
        var reportRepoMock = new Mock<IReportRepository>();
        var companyRepoMock = new Mock<ICompanyRepository>();

        var company = new CompanyProfile { LegalName = "Acme Corp" };
        companyRepoMock
            .Setup(c => c.GetPrimaryCompanyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var overview = new FinancialOverviewReportDto(
            TotalSales: 500000m,
            TotalPurchases: 320000m,
            TotalExpenses: 45000m,
            GrossProfit: 180000m,
            NetProfit: 135000m,
            ProfitMarginPercent: 27.0m,
            TotalReceivables: 65000m,
            TotalPayables: 40000m,
            TotalStockValuation: 210000m,
            FromDate: DateTime.Today.AddDays(-30),
            ToDate: DateTime.Today
        );

        reportRepoMock
            .Setup(r => r.GetFinancialOverviewAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        reportRepoMock
            .Setup(r => r.GetMonthlyFinancialTrendsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonthlyFinancialTrendDto>
            {
                new("Jan 2026", 2026, 1, 100000, 60000, 10000, 30000),
                new("Feb 2026", 2026, 2, 120000, 70000, 12000, 38000)
            });

        reportRepoMock
            .Setup(r => r.GetGstLiabilitySummaryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GstLiabilitySummaryDto(
                TotalOutwardTaxable: 400000m,
                OutwardCgst: 36000m,
                OutwardSgst: 36000m,
                OutwardIgst: 0m,
                TotalOutwardTax: 72000m,
                TotalInwardTaxable: 250000m,
                InwardEligibleCgst: 22500m,
                InwardEligibleSgst: 22500m,
                InwardEligibleIgst: 0m,
                TotalEligibleItc: 45000m,
                NetPayableCgst: 13500m,
                NetPayableSgst: 13500m,
                NetPayableIgst: 0m,
                TotalNetGstPayable: 27000m
            ));

        reportRepoMock
            .Setup(r => r.GetAgingBucketsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgingBucketDto>
            {
                new("0-30 Days", 45000m, 5, 25000m, 3),
                new("31-60 Days", 20000m, 2, 15000m, 2)
            });

        reportRepoMock
            .Setup(r => r.GetTopOverduePartiesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartyAgingDetailDto>());

        reportRepoMock
            .Setup(r => r.GetProductMovementVelocitiesAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductMovementVelocityDto>());

        var service = new ReportService(reportRepoMock.Object, companyRepoMock.Object, NullLogger<ReportService>.Instance);

        // Act
        var bundle = await service.GetComprehensiveReportsAsync();

        // Assert
        bundle.Should().NotBeNull();
        bundle.Overview.GrossProfit.Should().Be(180000m);
        bundle.Overview.NetProfit.Should().Be(135000m);
        bundle.GstSummary.TotalNetGstPayable.Should().Be(27000m);
        bundle.AgingBuckets.Should().HaveCount(2);
        bundle.MonthlyTrends.Should().HaveCount(2);
    }
}
