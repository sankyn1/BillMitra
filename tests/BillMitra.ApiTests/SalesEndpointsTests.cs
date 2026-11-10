using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Sales;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class SalesEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SalesEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CalculateGst_ReturnsOkWithAccurateTotals()
    {
        var req = new CalculateGstRequest(
            SupplierStateCode: "24",
            PlaceOfSupplyStateCode: "24",
            IsReverseCharge: false,
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Test Item", "8471", 2, "PCS", 1000m, 0, 18m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/v1/sales/calculate", req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CalculateGstResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.SubTotalTaxable.Should().Be(2000m);
        body.Data.GrandTotal.Should().Be(2360m);
    }

    [Fact]
    public async Task CreateDraft_ThenFinalize_ThenDownloadPdf_FlowSucceeds()
    {
        // 1. Create Draft
        var createReq = new CreateOrUpdateSalesDocumentRequest(
            DocumentType: DocumentType.TaxInvoice,
            DocumentDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddDays(30),
            PartyId: null,
            CustomerName: "Reliance Retail Ltd",
            CustomerGstin: "24AAACR5055K1Z8",
            BillingAddress: "GIFT City, Gandhinagar, Gujarat",
            ShippingAddress: "GIFT City, Gandhinagar, Gujarat",
            PlaceOfSupplyStateCode: "24",
            IsReverseCharge: false,
            Notes: "Payment due in 30 days",
            TermsAndConditions: "Goods once sold will not be taken back",
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Server Rack 42U", "8537", 1, "SET", 45000m, 5m, 18m)
            }
        );

        var draftRes = await _client.PostAsJsonAsync("/api/v1/sales/draft", createReq);
        draftRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var draftBody = await draftRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        draftBody.Should().NotBeNull();
        draftBody!.Success.Should().BeTrue();
        var docId = draftBody.Data!.Id;
        draftBody.Data.DocumentStatus.Should().Be(DocumentStatus.Draft);
        draftBody.Data.DraftNumber.Should().StartWith("DFT-");

        // 2. Finalize Document
        var finalizeReq = new FinalizeDocumentRequest(IdempotencyKey: Guid.NewGuid().ToString());
        var finalizeRes = await _client.PostAsJsonAsync($"/api/v1/sales/{docId}/finalize", finalizeReq);
        finalizeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalBody = await finalizeRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        finalBody.Should().NotBeNull();
        finalBody!.Success.Should().BeTrue();
        finalBody.Data!.DocumentStatus.Should().Be(DocumentStatus.Finalized);
        finalBody.Data.DocumentNumber.Should().NotBeNullOrWhiteSpace();
        finalBody.Data.DocumentNumber.Should().StartWith("INV/");

        // 3. Download PDF
        var pdfRes = await _client.GetAsync($"/api/v1/sales/{docId}/pdf?format=a4");
        var errStr = await pdfRes.Content.ReadAsStringAsync();
        pdfRes.StatusCode.Should().Be(HttpStatusCode.OK, because: $"PDF generation failed with error: {errStr}");
        pdfRes.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        pdfBytes.Length.Should().BeGreaterThan(1000); // Valid generated PDF document
    }

    [Fact]
    public async Task Cancel_WithoutReason_ReturnsBadRequest()
    {
        var cancelReq = new CancelDocumentRequest(CancellationReason: "");
        var res = await _client.PostAsJsonAsync($"/api/v1/sales/{Guid.NewGuid()}/cancel", cancelReq);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertProformaToInvoice_FlowSucceeds_AndLinksSource()
    {
        // 1. Create a Proforma Invoice draft
        var createProformaReq = new CreateOrUpdateSalesDocumentRequest(
            DocumentType: DocumentType.ProformaInvoice,
            DocumentDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddDays(15),
            PartyId: null,
            CustomerName: "Tata Consultancy Services Ltd",
            CustomerGstin: "27AAACT2727Q1ZW",
            BillingAddress: "TCS House, Raveline Street, Fort, Mumbai",
            ShippingAddress: null,
            PlaceOfSupplyStateCode: "27",
            IsReverseCharge: false,
            Notes: "Proforma Estimate for Cloud Setup",
            TermsAndConditions: "Validity: 15 days",
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Annual Cloud Support Contract", "998313", 1, "YR", 120000m, 10m, 18m)
            }
        );

        var proformaRes = await _client.PostAsJsonAsync("/api/v1/sales/draft", createProformaReq);
        proformaRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var proformaBody = await proformaRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        proformaBody.Should().NotBeNull();
        var proforma = proformaBody!.Data!;
        proforma.DocumentType.Should().Be(DocumentType.ProformaInvoice);
        proforma.DocumentStatus.Should().Be(DocumentStatus.Draft);

        // 2. Convert Proforma to Tax Invoice
        var convertReq = new ConvertToInvoiceRequest(FinalizeImmediately: false);
        var convertRes = await _client.PostAsJsonAsync($"/api/v1/sales/{proforma.Id}/convert-to-invoice", convertReq);
        convertRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var convertBody = await convertRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        convertBody.Should().NotBeNull();
        var newInvoice = convertBody!.Data!;
        newInvoice.DocumentType.Should().Be(DocumentType.TaxInvoice);
        newInvoice.SourceDocumentId.Should().Be(proforma.Id);
        newInvoice.SourceDocumentNumber.Should().Be(proforma.DocumentNumber ?? proforma.DraftNumber);
        newInvoice.CustomerName.Should().Be(proforma.CustomerName);
        newInvoice.GrandTotal.Should().Be(proforma.GrandTotal);

        // 3. Verify Source Proforma status is now Converted
        var fetchSourceRes = await _client.GetAsync($"/api/v1/sales/{proforma.Id}");
        fetchSourceRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var sourceBody = await fetchSourceRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        sourceBody.Should().NotBeNull();
        sourceBody!.Data!.DocumentStatus.Should().Be(DocumentStatus.Converted);
        sourceBody.Data.ConvertedToDocumentId.Should().Be(newInvoice.Id);
        sourceBody.Data.CanConvert.Should().BeFalse();

        // 4. Attempting duplicate conversion should be rejected
        var duplicateRes = await _client.PostAsJsonAsync($"/api/v1/sales/{proforma.Id}/convert-to-invoice", convertReq);
        duplicateRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LetterheadSettings_UpdateAndPreprintedPdfGeneration_Succeeds()
    {
        // 1. Update letterhead mode to Preprinted stationery
        var settingsReq = new BillMitra.Contracts.Company.UpdateLetterheadSettingsRequest(
            DefaultLetterheadMode: 2,
            PreprintedHeaderMarginMm: 55
        );
        var updateRes = await _client.PutAsJsonAsync("/api/v1/company/letterhead-settings", settingsReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var profileRes = await _client.GetAsync("/api/v1/company/profile");
        profileRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var profileBody = await profileRes.Content.ReadFromJsonAsync<ApiResponse<BillMitra.Contracts.Company.CompanyProfileDto>>();
        profileBody.Should().NotBeNull();
        profileBody!.Data!.DefaultLetterheadMode.Should().Be(2);
        profileBody.Data.PreprintedHeaderMarginMm.Should().Be(55);

        // 2. Create invoice and verify pre-printed stationery PDF generation
        var createReq = new CreateOrUpdateSalesDocumentRequest(
            DocumentType: DocumentType.TaxInvoice,
            DocumentDate: DateTime.UtcNow,
            DueDate: null,
            PartyId: null,
            CustomerName: "Larsen & Toubro Ltd",
            CustomerGstin: "27AAACL0149P1ZZ",
            BillingAddress: "Powai, Mumbai",
            ShippingAddress: null,
            PlaceOfSupplyStateCode: "27",
            IsReverseCharge: false,
            Notes: null,
            TermsAndConditions: null,
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Industrial Valves", "8481", 4, "NOS", 7500m, 0, 18m)
            }
        );
        var draftRes = await _client.PostAsJsonAsync("/api/v1/sales/draft", createReq);
        var draftBody = await draftRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        var docId = draftBody!.Data!.Id;

        // Generate PDF without letterhead (pre-printed stationery)
        var pdfRes = await _client.GetAsync($"/api/v1/sales/{docId}/pdf?format=a4&withLetterhead=false");
        pdfRes.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfRes.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        pdfBytes.Length.Should().BeGreaterThan(1000);
    }
}
