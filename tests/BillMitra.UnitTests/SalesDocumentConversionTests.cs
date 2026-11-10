using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BillMitra.UnitTests;

public class SalesDocumentConversionTests
{
    [Theory]
    [InlineData(DocumentType.ProformaInvoice, DocumentStatus.Draft, true)]
    [InlineData(DocumentType.ProformaInvoice, DocumentStatus.Finalized, true)]
    [InlineData(DocumentType.Quotation, DocumentStatus.Draft, true)]
    [InlineData(DocumentType.Quotation, DocumentStatus.Finalized, true)]
    [InlineData(DocumentType.ProformaInvoice, DocumentStatus.Converted, false)]
    [InlineData(DocumentType.ProformaInvoice, DocumentStatus.Cancelled, false)]
    [InlineData(DocumentType.Quotation, DocumentStatus.Converted, false)]
    [InlineData(DocumentType.Quotation, DocumentStatus.Cancelled, false)]
    [InlineData(DocumentType.TaxInvoice, DocumentStatus.Draft, false)]
    [InlineData(DocumentType.TaxInvoice, DocumentStatus.Finalized, false)]
    [InlineData(DocumentType.BillOfSupply, DocumentStatus.Draft, false)]
    [InlineData(DocumentType.CreditNote, DocumentStatus.Finalized, false)]
    [InlineData(DocumentType.DebitNote, DocumentStatus.Finalized, false)]
    public void CanConvert_ShouldOnlyAllowActiveProformaOrQuotation(DocumentType docType, DocumentStatus docStatus, bool expectedResult)
    {
        var doc = new SalesDocument
        {
            DocumentType = docType,
            DocumentStatus = docStatus,
            CustomerName = "Test Enterprises"
        };

        doc.CanConvert().Should().Be(expectedResult);
    }

    [Fact]
    public void CompanyProfile_StationeryDefaults_ShouldBeValid()
    {
        var company = new CompanyProfile
        {
            LegalName = "BillMitra Solutions Pvt Ltd",
            StateCode = "24",
            StateName = "Gujarat",
            DefaultLetterheadMode = 1,
            PreprintedHeaderMarginMm = 50
        };

        company.DefaultLetterheadMode.Should().Be(1);
        company.PreprintedHeaderMarginMm.Should().Be(50);
        company.LetterheadImagePath.Should().BeNull();
    }
}

