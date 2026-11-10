using BillMitra.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class CompanyProfileTests
{
    [Theory]
    [InlineData("24ABCDE1234F1Z5", true)]
    [InlineData("27AAPFU0939F1ZV", true)]
    [InlineData("07AAAAA0000A1Z5", true)]
    [InlineData("INVALIDGSTIN123", false)]
    [InlineData("24ABCDE1234F1Z", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidGstin_ValidatesCorrectly(string? gstin, bool expected)
    {
        CompanyProfile.IsValidGstin(gstin).Should().Be(expected);
    }

    [Fact]
    public void ExtractPanFromGstin_ReturnsAccurate10CharPan()
    {
        var pan = CompanyProfile.ExtractPanFromGstin("24ABCDE1234F1Z5");
        pan.Should().Be("ABCDE1234F");
    }

    [Fact]
    public void ExtractStateCodeFromGstin_ReturnsFirstTwoDigits()
    {
        var state = CompanyProfile.ExtractStateCodeFromGstin("24ABCDE1234F1Z5");
        state.Should().Be("24");
    }
}

public class FinancialYearTests
{
    [Fact]
    public void CreateDefaultIndianFinancialYear_GeneratesCorrectDatesAndCode()
    {
        var fy = FinancialYear.CreateDefaultIndianFinancialYear(2026);

        fy.Code.Should().Be("2026-27");
        fy.StartDate.Should().Be(new DateOnly(2026, 4, 1));
        fy.EndDate.Should().Be(new DateOnly(2027, 3, 31));
        fy.IsCurrent.Should().BeTrue();
        fy.IsClosed.Should().BeFalse();
    }
}

