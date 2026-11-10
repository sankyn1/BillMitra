using BillMitra.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class ProductMasterTests
{
    [Theory]
    [InlineData("8471", true)]
    [InlineData("847130", true)]
    [InlineData("84713010", true)]
    [InlineData("10", true)]
    [InlineData("847", false)]     // 3 digits invalid
    [InlineData("84713", false)]   // 5 digits invalid
    [InlineData("84A1", false)]    // non-digit invalid
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidHsn_ValidatesCorrectly(string? hsn, bool expected)
    {
        Product.IsValidHsn(hsn).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(12, true)]
    [InlineData(18, true)]
    [InlineData(28, true)]
    [InlineData(7, false)]
    [InlineData(25, false)]
    public void ValidGstRates_ContainsOnlyApprovedTiers(decimal rate, bool expected)
    {
        Product.ValidGstRates.Contains(rate).Should().Be(expected);
    }
}
