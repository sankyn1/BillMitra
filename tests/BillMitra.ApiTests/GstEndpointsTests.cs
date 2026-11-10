using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Gst;
using BillMitra.Contracts.Sales;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class GstEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GstEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GstEndpoints_FullReturnsWorkflow_Succeeds()
    {
        var period = $"{DateTime.Today.Month:D2}{DateTime.Today.Year}";

        // 1. Create and Finalize a B2B Invoice
        var b2bReq = new CreateOrUpdateSalesDocumentRequest(
            DocumentType: DocumentType.TaxInvoice,
            DocumentDate: DateTime.Today,
            DueDate: DateTime.Today.AddDays(30),
            PartyId: null,
            CustomerName: "Infosys Technologies Ltd",
            CustomerGstin: "29AAACI4818K1ZV",
            BillingAddress: "Electronics City, Bengaluru",
            ShippingAddress: null,
            PlaceOfSupplyStateCode: "29", // Inter-State from Gujarat
            IsReverseCharge: false,
            Notes: "Consulting Services",
            TermsAndConditions: "Standard Terms",
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Enterprise Software Setup", "998314", 1, "SET", 80000m, 0, 18m)
            }
        );

        var draftRes = await _client.PostAsJsonAsync("/api/v1/sales/draft", b2bReq);
        draftRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var draft = (await draftRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>())!.Data!;

        var finalizeRes = await _client.PostAsJsonAsync($"/api/v1/sales/{draft.Id}/finalize", new FinalizeDocumentRequest());
        finalizeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Fetch GSTR-1 Summary
        var gstr1Res = await _client.GetAsync($"/api/v1/gst/gstr1/summary?taxPeriod={period}");
        gstr1Res.StatusCode.Should().Be(HttpStatusCode.OK);

        var gstr1Body = await gstr1Res.Content.ReadFromJsonAsync<ApiResponse<Gstr1SummaryDto>>();
        gstr1Body.Should().NotBeNull();
        gstr1Body!.Success.Should().BeTrue();
        var gstr1 = gstr1Body.Data!;
        gstr1.B2bCount.Should().BeGreaterThanOrEqualTo(1);
        gstr1.TotalTaxableValue.Should().BeGreaterThanOrEqualTo(80000m);

        // 3. Validate GSTR-1
        var validateRes = await _client.GetAsync($"/api/v1/gst/gstr1/validate?taxPeriod={period}");
        validateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var validateBody = await validateRes.Content.ReadFromJsonAsync<ApiResponse<GstValidationResultDto>>();
        validateBody.Should().NotBeNull();
        validateBody!.Success.Should().BeTrue();
        validateBody.Data!.IsValid.Should().BeTrue();

        // 4. Download GSTR-1 Portal Offline Tool JSON
        var jsonRes = await _client.GetAsync($"/api/v1/gst/gstr1/json?taxPeriod={period}");
        jsonRes.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonRes.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var jsonBytes = await jsonRes.Content.ReadAsByteArrayAsync();
        jsonBytes.Length.Should().BeGreaterThan(50);

        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        var parsedRoot = JsonSerializer.Deserialize<Gstr1JsonRoot>(jsonString);
        parsedRoot.Should().NotBeNull();
        parsedRoot!.fp.Should().Be(period);
        parsedRoot.b2b.Should().NotBeEmpty();

        // 5. Fetch GSTR-3B Summary
        var gstr3bRes = await _client.GetAsync($"/api/v1/gst/gstr3b/summary?taxPeriod={period}");
        gstr3bRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var gstr3bBody = await gstr3bRes.Content.ReadFromJsonAsync<ApiResponse<Gstr3bSummaryDto>>();
        gstr3bBody.Should().NotBeNull();
        gstr3bBody!.Success.Should().BeTrue();
        gstr3bBody.Data!.Table31.OutwardTaxableSupplies.TaxableValue.Should().BeGreaterThanOrEqualTo(80000m);

        // 6. Fetch Table 12 HSN Summary
        var hsnRes = await _client.GetAsync($"/api/v1/gst/hsn-summary?taxPeriod={period}");
        hsnRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var hsnBody = await hsnRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<HsnSummaryItemDto>>>();
        hsnBody.Should().NotBeNull();
        hsnBody!.Data.Should().Contain(h => h.HsnSacCode == "998314");

        // 7. Submit GSTR-1 to Sandbox Provider
        var submitReq = new SubmitGstReturnRequest(TaxPeriod: period, SimulateProvider: true);
        var submitRes = await _client.PostAsJsonAsync("/api/v1/gst/gstr1/submit", submitReq);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitBody = await submitRes.Content.ReadFromJsonAsync<ApiResponse<GstReturnFilingDto>>();
        submitBody.Should().NotBeNull();
        submitBody!.Success.Should().BeTrue();
        submitBody.Data!.FilingStatus.Should().Be(GstFilingStatusDto.Filed);
        submitBody.Data.Arn.Should().NotBeNullOrWhiteSpace();
        submitBody.Data.Arn.Should().StartWith("AA");

        // 8. Fetch Filing History
        var historyRes = await _client.GetAsync("/api/v1/gst/filings");
        historyRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyBody = await historyRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<GstReturnFilingDto>>>();
        historyBody.Should().NotBeNull();
        historyBody!.Data.Should().Contain(f => f.TaxPeriod == period);
    }
}
