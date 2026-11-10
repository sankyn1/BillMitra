using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.EWayInvoice;
using BillMitra.Contracts.Sales;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class EWayInvoiceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EWayInvoiceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EWayInvoice_FullWorkflow_SucceedsEndToEnd()
    {
        // 1. Create and Finalize a B2B Invoice (> ₹50,000 consignment value)
        var b2bReq = new CreateOrUpdateSalesDocumentRequest(
            DocumentType: DocumentType.TaxInvoice,
            DocumentDate: DateTime.Today,
            DueDate: DateTime.Today.AddDays(30),
            PartyId: null,
            CustomerName: "Tata Consultancy Services Ltd",
            CustomerGstin: "27AAACT2727Q1ZW", // Maharashtra B2B
            BillingAddress: "TCS House, Raveline Street, Fort, Mumbai",
            ShippingAddress: null,
            PlaceOfSupplyStateCode: "27",
            IsReverseCharge: false,
            Notes: "Hardware Supply for Data Center",
            TermsAndConditions: "30 days net",
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Server Rack 42U", "851762", 2, "NOS", 45000m, 0, 18m)
            }
        );

        var draftRes = await _client.PostAsJsonAsync("/api/v1/sales/draft", b2bReq);
        draftRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var draft = (await draftRes.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>())!.Data!;

        var finalizeRes = await _client.PostAsJsonAsync($"/api/v1/sales/{draft.Id}/finalize", new FinalizeDocumentRequest());
        finalizeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Generate E-Way Bill
        var genEwbReq = new GenerateEWayBillRequest(
            SalesDocumentId: draft.Id,
            TransportationMode: 1, // Road
            TransporterId: "24AAACG1234A1Z5",
            TransporterName: "VRL Logistics Ltd",
            TransportDocNo: "VRL-882194",
            TransportDocDate: DateTime.Today,
            VehicleNo: "GJ01AB9876",
            VehicleType: 1, // Regular
            ApproximateDistanceKm: 450
        );

        var ewbRes = await _client.PostAsJsonAsync("/api/v1/ewaybill/generate", genEwbReq);
        ewbRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var ewb = (await ewbRes.Content.ReadFromJsonAsync<ApiResponse<EWayBillDto>>())!.Data!;

        ewb.EWayBillNumber.Should().HaveLength(12);
        ewb.VehicleNo.Should().Be("GJ01AB9876");
        ewb.ValidUntil.Should().BeAfter(ewb.EWayBillDate);

        // 3. Update Vehicle / Part-B
        var updateVehReq = new UpdatePartBRequest(
            EWayBillNumber: ewb.EWayBillNumber,
            VehicleNo: "MH04CD1234",
            FromPlace: "Surat",
            FromStateCode: "24",
            ReasonCode: "1", // Breakdown
            Remarks: "Engine breakdown in transit"
        );

        var updateRes = await _client.PostAsJsonAsync("/api/v1/ewaybill/update-vehicle", updateVehReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedEwb = (await updateRes.Content.ReadFromJsonAsync<ApiResponse<EWayBillDto>>())!.Data!;
        updatedEwb.VehicleNo.Should().Be("MH04CD1234");
        updatedEwb.StatusName.Should().Be("Updated");

        // 4. Generate E-Invoice (IRN)
        var einvReq = new GenerateEInvoiceRequest(SalesDocumentId: draft.Id);
        var einvRes = await _client.PostAsJsonAsync("/api/v1/einvoice/generate", einvReq);
        einvRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var einv = (await einvRes.Content.ReadFromJsonAsync<ApiResponse<EInvoiceDto>>())!.Data!;

        einv.Irn.Should().HaveLength(64);
        einv.AckNo.Should().HaveLength(15);
        einv.SignedQrCode.Should().NotBeNullOrWhiteSpace();

        // 5. Get Combined Document Summary
        var summaryRes = await _client.GetAsync($"/api/v1/einvoice/document/{draft.Id}/summary");
        summaryRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = (await summaryRes.Content.ReadFromJsonAsync<ApiResponse<DocumentEWayInvoiceSummaryDto>>())!.Data!;

        summary.IsEWayBillMandatory.Should().BeTrue();
        summary.IsEInvoiceEligible.Should().BeTrue();
        summary.EWayBill.Should().NotBeNull();
        summary.EWayBill!.EWayBillNumber.Should().Be(ewb.EWayBillNumber);
        summary.EInvoice.Should().NotBeNull();
        summary.EInvoice!.Irn.Should().Be(einv.Irn);

        // 6. Cancel E-Way Bill
        var cancelEwbReq = new CancelEWayBillRequest(
            EWayBillNumber: ewb.EWayBillNumber,
            CancelReasonCode: "1",
            CancelRemarks: "Order cancelled by customer"
        );

        var cancelEwbRes = await _client.PostAsJsonAsync("/api/v1/ewaybill/cancel", cancelEwbReq);
        cancelEwbRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Cancel E-Invoice
        var cancelEinvReq = new CancelEInvoiceRequest(
            Irn: einv.Irn,
            CancelReason: 3, // Order Cancelled
            CancelRemarks: "Consignment cancelled"
        );

        var cancelEinvRes = await _client.PostAsJsonAsync("/api/v1/einvoice/cancel", cancelEinvReq);
        cancelEinvRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
