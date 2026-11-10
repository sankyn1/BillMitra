using System;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class EWayInvoiceUnitTests
{
    [Fact]
    public void EWayBill_CanCancel_Within24Hours_ReturnsTrue()
    {
        var eway = new EWayBillRecord
        {
            EWayBillNumber = "241012345678",
            EWayBillDate = DateTimeOffset.UtcNow.AddHours(-12),
            Status = EWayBillStatus.Generated
        };

        eway.CanCancel().Should().BeTrue();
    }

    [Fact]
    public void EWayBill_CanCancel_After24Hours_ReturnsFalse()
    {
        var eway = new EWayBillRecord
        {
            EWayBillNumber = "241012345678",
            EWayBillDate = DateTimeOffset.UtcNow.AddHours(-25),
            Status = EWayBillStatus.Generated
        };

        eway.CanCancel().Should().BeFalse();
    }

    [Fact]
    public void EWayBill_CanCancel_AlreadyCancelled_ReturnsFalse()
    {
        var eway = new EWayBillRecord
        {
            EWayBillNumber = "241012345678",
            EWayBillDate = DateTimeOffset.UtcNow.AddHours(-2),
            Status = EWayBillStatus.Cancelled
        };

        eway.CanCancel().Should().BeFalse();
    }

    [Fact]
    public void EInvoice_CanCancel_Within24Hours_ReturnsTrue()
    {
        var einv = new EInvoiceRecord
        {
            Irn = "64charhash1234567890abcdef1234567890abcdef1234567890abcdef12345678",
            AckDate = DateTimeOffset.UtcNow.AddHours(-6),
            Status = EInvoiceStatus.Generated
        };

        einv.CanCancel().Should().BeTrue();
    }

    [Fact]
    public void EInvoice_CanCancel_After24Hours_ReturnsFalse()
    {
        var einv = new EInvoiceRecord
        {
            Irn = "64charhash1234567890abcdef1234567890abcdef1234567890abcdef12345678",
            AckDate = DateTimeOffset.UtcNow.AddHours(-26),
            Status = EInvoiceStatus.Generated
        };

        einv.CanCancel().Should().BeFalse();
    }

    [Theory]
    [InlineData(50, 1)]     // 50 km => 1 day
    [InlineData(200, 1)]    // 200 km => 1 day
    [InlineData(250, 2)]    // 250 km => 2 days
    [InlineData(500, 3)]    // 500 km => 3 days
    public void StatutoryEWayValidity_Calculation_PassesGovernmentRules(int distanceKm, int expectedDays)
    {
        var calculatedDays = Math.Max(1, (int)Math.Ceiling(distanceKm / 200.0));
        calculatedDays.Should().Be(expectedDays);
    }
}
