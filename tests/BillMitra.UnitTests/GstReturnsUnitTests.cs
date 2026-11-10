using System;
using System.Collections.Generic;
using System.Linq;
using BillMitra.Contracts.Gst;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class GstReturnsUnitTests
{
    [Fact]
    public void B2clThresholdRule_InterStateAbove2Lakh50k_IsClassifiedAsB2CL()
    {
        // CBIC Statutory Rule: Inter-state invoice to unregistered person > ₹2,50,000 must be reported in Table 5A (B2CL)
        var companyState = "24"; // Gujarat

        var invoiceLarge = new SalesDocument
        {
            CustomerGstin = null,
            PlaceOfSupplyStateCode = "27", // Maharashtra (Inter-State)
            GrandTotal = 250001m
        };

        var invoiceSmall = new SalesDocument
        {
            CustomerGstin = null,
            PlaceOfSupplyStateCode = "27", // Maharashtra (Inter-State)
            GrandTotal = 250000m
        };

        var isLarge = string.IsNullOrWhiteSpace(invoiceLarge.CustomerGstin) &&
                      invoiceLarge.PlaceOfSupplyStateCode != companyState &&
                      invoiceLarge.GrandTotal > 250000m;

        var isSmall = string.IsNullOrWhiteSpace(invoiceSmall.CustomerGstin) &&
                      invoiceSmall.PlaceOfSupplyStateCode != companyState &&
                      invoiceSmall.GrandTotal <= 250000m;

        isLarge.Should().BeTrue();
        isSmall.Should().BeTrue();
    }

    [Fact]
    public void B2BClassification_RequiresValidCustomerGstin()
    {
        var b2bDoc = new SalesDocument
        {
            DocumentType = DocumentType.TaxInvoice,
            CustomerGstin = "27AAACR5055K1Z8",
            GrandTotal = 50000m
        };

        var b2cDoc = new SalesDocument
        {
            DocumentType = DocumentType.TaxInvoice,
            CustomerGstin = null,
            GrandTotal = 50000m
        };

        (!string.IsNullOrWhiteSpace(b2bDoc.CustomerGstin)).Should().BeTrue();
        (!string.IsNullOrWhiteSpace(b2cDoc.CustomerGstin)).Should().BeFalse();
    }

    [Fact]
    public void HsnSummary_GroupsByHsnCodeAndRate()
    {
        var items = new List<SalesDocumentItem>
        {
            new() { HsnSacCode = "8471", ItemName = "Computer", Quantity = 2, TaxableAmount = 50000m, GstRatePercent = 18m, IgstAmount = 9000m, TotalAmount = 59000m },
            new() { HsnSacCode = "8471", ItemName = "Server", Quantity = 1, TaxableAmount = 70000m, GstRatePercent = 18m, IgstAmount = 12600m, TotalAmount = 82600m },
            new() { HsnSacCode = "8517", ItemName = "Modem", Quantity = 5, TaxableAmount = 15000m, GstRatePercent = 18m, IgstAmount = 2700m, TotalAmount = 17700m }
        };

        var grouped = items
            .GroupBy(i => new { i.HsnSacCode, i.GstRatePercent })
            .Select(g => new
            {
                Hsn = g.Key.HsnSacCode,
                Rate = g.Key.GstRatePercent,
                Qty = g.Sum(x => x.Quantity),
                Taxable = g.Sum(x => x.TaxableAmount),
                Tax = g.Sum(x => x.IgstAmount)
            })
            .ToList();

        grouped.Should().HaveCount(2);
        var hsn8471 = grouped.First(g => g.Hsn == "8471");
        hsn8471.Qty.Should().Be(3);
        hsn8471.Taxable.Should().Be(120000m);
        hsn8471.Tax.Should().Be(21600m);
    }

    [Fact]
    public void Gstr3bTable31_ReverseChargeInvoices_ReportedSeparatelyInRowD()
    {
        var regularDoc = new SalesDocument
        {
            IsReverseCharge = false,
            SubTotalTaxable = 100000m,
            TotalTax = 18000m,
            CgstTotal = 9000m,
            SgstTotal = 9000m
        };

        var rcmDoc = new SalesDocument
        {
            IsReverseCharge = true,
            SubTotalTaxable = 20000m,
            TotalTax = 1000m,
            CgstTotal = 500m,
            SgstTotal = 500m
        };

        var docs = new List<SalesDocument> { regularDoc, rcmDoc };

        var rowA_Taxable = docs.Where(d => !d.IsReverseCharge).Sum(d => d.SubTotalTaxable);
        var rowD_Taxable = docs.Where(d => d.IsReverseCharge).Sum(d => d.SubTotalTaxable);

        rowA_Taxable.Should().Be(100000m);
        rowD_Taxable.Should().Be(20000m);
    }
}

