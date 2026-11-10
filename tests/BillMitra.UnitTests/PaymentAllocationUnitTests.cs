using System;
using System.Collections.Generic;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Xunit;

namespace BillMitra.UnitTests;

public class PaymentAllocationUnitTests
{
    [Fact]
    public void Payment_Should_Track_Allocated_And_Unallocated_Amount_Correctly()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = "RCP/26-27/0001",
            Amount = 10000m,
            PaymentType = PaymentType.Receipt,
            PaymentMode = PaymentMode.UPI
        };

        Assert.Equal(10000m, payment.Amount);
        Assert.Equal(0m, payment.AllocatedAmount);
        Assert.Equal(10000m, payment.UnallocatedAmount);

        var invoice1Id = Guid.NewGuid();
        payment.AddAllocation(invoice1Id, 4500m, "Partial settlement for Inv 1");

        Assert.Equal(4500m, payment.AllocatedAmount);
        Assert.Equal(5500m, payment.UnallocatedAmount);

        var invoice2Id = Guid.NewGuid();
        payment.AddAllocation(invoice2Id, 5500m, "Full settlement for Inv 2");

        Assert.Equal(10000m, payment.AllocatedAmount);
        Assert.Equal(0m, payment.UnallocatedAmount);
    }

    [Fact]
    public void Payment_AddAllocation_Should_Throw_When_Allocating_More_Than_Available()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = "RCP/26-27/0002",
            Amount = 5000m,
            PaymentType = PaymentType.Receipt
        };

        var invoiceId = Guid.NewGuid();
        var ex = Assert.Throws<InvalidOperationException>(() => payment.AddAllocation(invoiceId, 5001m));

        Assert.Contains("Cannot allocate", ex.Message);
    }

    [Fact]
    public void Payment_AddAllocation_Should_Throw_When_Allocating_Negative_Or_Zero()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 5000m
        };

        Assert.Throws<ArgumentException>(() => payment.AddAllocation(Guid.NewGuid(), 0m));
        Assert.Throws<ArgumentException>(() => payment.AddAllocation(Guid.NewGuid(), -100m));
    }

    [Theory]
    [InlineData(10, "0-30")]
    [InlineData(30, "0-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "90+")]
    [InlineData(180, "90+")]
    public void AgingBucket_Should_Categorize_Correctly(int daysOld, string expectedBucket)
    {
        var bucket = daysOld switch
        {
            <= 30 => "0-30",
            <= 60 => "31-60",
            <= 90 => "61-90",
            _ => "90+"
        };

        Assert.Equal(expectedBucket, bucket);
    }
}

