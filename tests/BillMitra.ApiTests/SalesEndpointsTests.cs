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
}
