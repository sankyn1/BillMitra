using BillMitra.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Money_Addition_ShouldCalculateCorrectSum()
    {
        var m1 = new Money(100.50m, "INR");
        var m2 = new Money(49.50m, "INR");

        var result = m1 + m2;

        result.Amount.Should().Be(150.00m);
        result.Currency.Should().Be("INR");
    }

    [Fact]
    public void Money_Subtraction_ShouldCalculateCorrectDifference()
    {
        var m1 = new Money(100.00m, "INR");
        var m2 = new Money(25.75m, "INR");

        var result = m1 - m2;

        result.Amount.Should().Be(74.25m);
    }

    [Fact]
    public void Money_Multiplication_ShouldScaleCorrectly()
    {
        var m1 = new Money(50.00m, "INR");

        var result = m1 * 1.18m; // 18% GST

        result.Amount.Should().Be(59.00m);
    }

    [Fact]
    public void Money_MismatchedCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = new Money(100.00m, "INR");
        var m2 = new Money(100.00m, "USD");

        var act = () => { var _ = m1 + m2; };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mismatched currencies*");
    }

    [Fact]
    public void Money_DivisionByZero_ShouldThrowDivideByZeroException()
    {
        var m1 = new Money(100.00m, "INR");

        var act = () => { var _ = m1 / 0m; };

        act.Should().Throw<DivideByZeroException>();
    }
}

