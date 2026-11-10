using System;
using System.Collections.Generic;
using BillMitra.Application.Services;
using BillMitra.Contracts.Sales;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class GstCalculationEngineTests
{
    private readonly SalesService _salesService;

    public GstCalculationEngineTests()
    {
        // SalesService calculation is pure, constructor dependencies can be null for CalculateGst
        _salesService = new SalesService(null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public void CalculateGst_IntraState_ApportionsCgstAndSgstEqually()
    {
        // Arrange: Supplier Gujarat (24) and Buyer Gujarat (24) -> IntraState
        var request = new CalculateGstRequest(
            SupplierStateCode: "24",
            PlaceOfSupplyStateCode: "24",
            IsReverseCharge: false,
            Items: new List<SalesDocumentItemInput>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ItemName: "Dell Latitude 5440",
                    HsnSacCode: "84713010",
                    Quantity: 2,
                    UnitCode: "NOS",
                    UnitPrice: 50000m,
                    DiscountPercent: 10m, // 10% disc = 10,000 -> Taxable = 90,000
                    GstRatePercent: 18m
                )
            }
        );

        // Act
        var result = _salesService.CalculateGst(request);

        // Assert
        result.SupplyType.Should().Be(GstSupplyType.IntraState);
        result.SubTotalTaxable.Should().Be(90000m);
        result.CgstTotal.Should().Be(8100m); // 9% of 90,000
        result.SgstTotal.Should().Be(8100m); // 9% of 90,000
        result.IgstTotal.Should().Be(0m);
        result.TotalTax.Should().Be(16200m);
        result.GrandTotal.Should().Be(106200m);
        result.RoundOffAmount.Should().Be(0m);
        result.TotalAmountInWords.Should().Contain("One Lakh Six Thousand Two Hundred");
    }

    [Fact]
    public void CalculateGst_InterState_AppliesFullIgst()
    {
        // Arrange: Supplier Gujarat (24) and Buyer Maharashtra (27) -> InterState
        var request = new CalculateGstRequest(
            SupplierStateCode: "24",
            PlaceOfSupplyStateCode: "27",
            IsReverseCharge: false,
            Items: new List<SalesDocumentItemInput>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ItemName: "Cloud Hosting Services",
                    HsnSacCode: "998315",
                    Quantity: 1,
                    UnitCode: "NOS",
                    UnitPrice: 25000m,
                    DiscountPercent: 0m,
                    GstRatePercent: 18m
                )
            }
        );

        // Act
        var result = _salesService.CalculateGst(request);

        // Assert
        result.SupplyType.Should().Be(GstSupplyType.InterState);
        result.SubTotalTaxable.Should().Be(25000m);
        result.CgstTotal.Should().Be(0m);
        result.SgstTotal.Should().Be(0m);
        result.IgstTotal.Should().Be(4500m); // 18% of 25,000
        result.TotalTax.Should().Be(4500m);
        result.GrandTotal.Should().Be(29500m);
        result.TotalAmountInWords.Should().Contain("Twenty Nine Thousand Five Hundred");
    }

    [Fact]
    public void CalculateGst_RoundingNearestRupee_PerRule46()
    {
        // Arrange: Item with fractional paise total (e.g. 10.33 * 3 = 30.99 + 18% = 36.5682)
        var request = new CalculateGstRequest(
            SupplierStateCode: "24",
            PlaceOfSupplyStateCode: "24",
            IsReverseCharge: false,
            Items: new List<SalesDocumentItemInput>
            {
                new(
                    ProductId: null,
                    ItemName: "Small Hardware Widget",
                    HsnSacCode: "73181500",
                    Quantity: 3,
                    UnitCode: "PCS",
                    UnitPrice: 10.33m,
                    DiscountPercent: 0m,
                    GstRatePercent: 18m
                )
            }
        );

        // Act
        var result = _salesService.CalculateGst(request);

        // Assert
        // Taxable = 30.99
        // CGST 9% of 30.99 = 2.7891 -> 2.79
        // SGST 9% of 30.99 = 2.7891 -> 2.79
        // Total Tax = 5.58
        // Raw Grand Total = 30.99 + 5.58 = 36.57
        // Nearest Rupee = 37.00
        // Round Off = +0.43
        result.GrandTotal.Should().Be(37.00m);
        result.RoundOffAmount.Should().Be(0.43m);
    }

    [Fact]
    public void IndianCurrencyHelper_ConvertsAmountToWordsAccurately()
    {
        IndianCurrencyHelper.ConvertToWords(1250.50m)
            .Should().Be("Rupees One Thousand Two Hundred Fifty and Fifty Paise Only");

        IndianCurrencyHelper.ConvertToWords(10000000m)
            .Should().Be("Rupees One Crore Only");
    }

    [Fact]
    public void DocumentSequence_FormatNumber_AdheresToRule46Constraints()
    {
        var seq = new DocumentSequence
        {
            DocumentType = BillMitra.Domain.Enums.DocumentType.TaxInvoice,
            Prefix = "INV",
            FinancialYearToken = "26-27",
            NextNumber = 42,
            PaddingLength = 4
        };

        var formatted = seq.FormatNumber(42);
        formatted.Should().Be("INV/26-27/0042");
        formatted.Length.Should().BeLessThanOrEqualTo(16);
    }
}
