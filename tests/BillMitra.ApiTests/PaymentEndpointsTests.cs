using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Masters;
using BillMitra.Contracts.Payments;
using BillMitra.Contracts.Sales;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class PaymentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PaymentAndLedgerWorkflow_SucceedsEndToEnd()
    {
        // 1. Create a customer party
        var partyReq = new CreateOrUpdatePartyRequest
        {
            Name = "Ahmedabad Enterprise " + Guid.NewGuid().ToString("N")[..6],
            PartyType = 1, // Customer
            Gstin = "24AAACA1234A1Z5",
            GstRegistrationType = 1,
            StateCode = "24",
            StateName = "Gujarat",
            AddressLine1 = "SG Highway",
            AddressLine2 = null,
            City = "Ahmedabad",
            Pincode = "380054",
            Phone = "9876543210",
            Email = "info@ahmedabadenterprise.com",
            CreditLimit = 500000
        };

        var partyResp = await _client.PostAsJsonAsync("/api/v1/parties", partyReq);
        partyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var partyBody = await partyResp.Content.ReadFromJsonAsync<ApiResponse<PartyDto>>();
        var partyId = partyBody!.Data!.Id;

        // 2. Create and finalize a sales invoice for this party of ₹11,800 (10,000 + 18% GST)
        var createDocReq = new CreateOrUpdateSalesDocumentRequest(
            DocumentType: DocumentType.TaxInvoice,
            DocumentDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddDays(30),
            PartyId: partyId,
            CustomerName: partyBody.Data.Name,
            CustomerGstin: partyBody.Data.Gstin,
            BillingAddress: "SG Highway, Ahmedabad",
            ShippingAddress: "SG Highway, Ahmedabad",
            PlaceOfSupplyStateCode: "24",
            IsReverseCharge: false,
            Notes: "Service invoice for testing",
            TermsAndConditions: "Standard 30 days terms",
            Items: new List<SalesDocumentItemInput>
            {
                new(null, "Consulting Service", "998311", 1, "HRS", 10000m, 0, 18m)
            }
        );

        var docResp = await _client.PostAsJsonAsync("/api/v1/sales/draft", createDocReq);
        docResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var docBody = await docResp.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        var docId = docBody!.Data!.Id;

        // Finalize invoice
        var finalizeResp = await _client.PostAsJsonAsync($"/api/v1/sales/{docId}/finalize", new FinalizeDocumentRequest());
        finalizeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Query open invoices for party
        var openInvResp = await _client.GetAsync($"/api/v1/payments/parties/{partyId}/open-invoices");
        openInvResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var openInvs = await openInvResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<OpenInvoiceForPaymentDto>>>();
        openInvs!.Data.Should().Contain(d => d.SalesDocumentId == docId);
        var openInv = openInvs.Data!.FirstOrDefault(d => d.SalesDocumentId == docId)!;
        openInv.GrandTotal.Should().Be(11800m);
        openInv.OutstandingBalance.Should().Be(11800m);

        // 4. Record a partial receipt of ₹6,800 allocated to this invoice
        var paymentReq = new CreatePaymentRequest(
            PartyId: partyId,
            PaymentType: PaymentType.Receipt,
            PaymentMode: PaymentMode.Upi,
            PaymentDate: DateTime.UtcNow,
            Amount: 6800m,
            ReferenceNumber: "UPI-TXN-987654",
            BankName: "HDFC Bank",
            ChequeDate: null,
            Notes: "Partial payment via UPI",
            Allocations: new List<PaymentAllocationItemRequest>
            {
                new(docId, 6800m, "Partial settlement")
            }
        );

        var payResp = await _client.PostAsJsonAsync("/api/v1/payments", paymentReq);
        payResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payBody = await payResp.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>();
        payBody!.Success.Should().BeTrue();
        payBody.Data!.Amount.Should().Be(6800m);
        payBody.Data.AllocatedAmount.Should().Be(6800m);
        payBody.Data.UnallocatedAmount.Should().Be(0m);
        var paymentId = payBody.Data.Id;

        // 5. Verify the invoice status is now PartiallyPaid and balance is ₹5,000
        var verifyDocResp = await _client.GetAsync($"/api/v1/sales/{docId}");
        var verifyDoc = await verifyDocResp.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        verifyDoc!.Data!.PaidAmount.Should().Be(6800m);
        verifyDoc.Data.DocumentStatus.Should().Be(DocumentStatus.PartiallyPaid);

        // 6. Verify Customer Ledger statement
        var ledgerResp = await _client.GetAsync($"/api/v1/payments/parties/{partyId}/ledger");
        ledgerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var ledgerBody = await ledgerResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CustomerLedgerEntryDto>>>();
        ledgerBody!.Data.Should().HaveCount(2); // 1 Invoice (Debit 11800), 1 Receipt (Credit 6800)
        ledgerBody.Data![0].VoucherType.Should().Be("Invoice");
        ledgerBody.Data[0].DebitAmount.Should().Be(11800m);
        ledgerBody.Data[1].VoucherType.Should().Be("Receipt");
        ledgerBody.Data[1].CreditAmount.Should().Be(6800m);
        ledgerBody.Data[1].RunningBalance.Should().Be(5000m);

        // 7. Verify Party Aging summary
        var agingResp = await _client.GetAsync($"/api/v1/payments/parties/{partyId}/aging");
        agingResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var agingBody = await agingResp.Content.ReadFromJsonAsync<ApiResponse<PartyAgingSummaryDto>>();
        agingBody!.Data!.CurrentOutstanding.Should().Be(5000m);
        agingBody.Data.Days0To30.Should().Be(5000m);

        // 8. Test Reversal of the payment
        var revResp = await _client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/reverse", new ReversePaymentRequest("Customer payment bounce"));
        revResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9. Verify invoice balance restored back to ₹11,800
        var afterRevDocResp = await _client.GetAsync($"/api/v1/sales/{docId}");
        var afterRevDoc = await afterRevDocResp.Content.ReadFromJsonAsync<ApiResponse<SalesDocumentDto>>();
        afterRevDoc!.Data!.PaidAmount.Should().Be(0m);
        afterRevDoc.Data.DocumentStatus.Should().Be(DocumentStatus.Finalized);
    }
}
