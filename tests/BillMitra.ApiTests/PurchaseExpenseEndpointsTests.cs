using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Purchases;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class PurchaseExpenseEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PurchaseExpenseEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PurchaseEndpoints_FullWorkflow_CreatesAndFinalizes()
    {
        // 1. Create Purchase Draft
        var req = new CreateOrUpdatePurchaseRequest(
            DocumentType: 1, // PurchaseInvoice
            SupplierId: null,
            SupplierName: "Gujarat Steel Works Pvt Ltd",
            SupplierGstin: "24AAACG8811A1Z5",
            SupplierAddress: "Plot 42, GIDC Naroda, Ahmedabad",
            SupplierStateCode: "24",
            PlaceOfSupplyStateCode: "24",
            SupplierInvoiceNumber: $"GSW-{DateTime.UtcNow.Ticks % 100000}",
            SupplierInvoiceDate: DateTime.Today,
            DocumentDate: DateTime.Today,
            DueDate: DateTime.Today.AddDays(30),
            IsReverseCharge: false,
            DefaultItcEligibility: 1, // EligibleOther
            Notes: "Factory raw materials",
            Items: new List<PurchaseDocumentItemInput>
            {
                new(
                    ProductId: null,
                    ItemName: "Steel Plates Grade A",
                    HsnCode: "7208",
                    Quantity: 5,
                    Unit: "MT",
                    UnitPrice: 40000m,
                    DiscountPercent: 5m, // Taxable: 190000
                    GstRate: 18m,         // 9% CGST (17100) + 9% SGST (17100)
                    ItcEligibility: 1
                )
            }
        );

        var createRes = await _client.PostAsJsonAsync("/api/v1/purchases", req);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createRes.Content.ReadFromJsonAsync<ApiResponse<PurchaseDocumentDto>>();
        createJson.Should().NotBeNull();
        createJson!.Success.Should().BeTrue();
        var purchase = createJson.Data!;
        purchase.SubTotalTaxable.Should().Be(190000m);
        purchase.TotalTax.Should().Be(34200m);
        purchase.GrandTotal.Should().Be(224200m);

        // 2. Query Purchase List
        var listRes = await _client.GetAsync("/api/v1/purchases");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PurchaseDocumentDto>>>();
        listJson!.Success.Should().BeTrue();
        listJson.Data.Should().Contain(p => p.Id == purchase.Id);

        // 3. Finalize Purchase
        var finalRes = await _client.PostAsJsonAsync($"/api/v1/purchases/{purchase.Id}/finalize", new FinalizePurchaseRequest("Audited and accepted"));
        finalRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalJson = await finalRes.Content.ReadFromJsonAsync<ApiResponse<PurchaseDocumentDto>>();
        finalJson!.Success.Should().BeTrue();
        finalJson.Data!.DocumentStatusName.Should().Be("Finalized");
    }

    [Fact]
    public async Task ExpenseEndpoints_FullWorkflow_CategoriesAndVouchers()
    {
        // 1. Get Categories (seeds should exist)
        var catRes = await _client.GetAsync("/api/v1/expenses/categories");
        catRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var catJson = await catRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>();
        catJson.Should().NotBeNull();
        catJson!.Success.Should().BeTrue();
        catJson.Data.Should().NotBeEmpty();

        var firstCat = catJson.Data![0];

        // 2. Record Expense
        var expReq = new CreateExpenseRequest(
            ExpenseCategoryId: firstCat.Id,
            ExpenseDate: DateTime.Today,
            Amount: 2500m,
            GstRate: 18m,
            IsItcClaimable: true,
            PaymentMode: 4, // UPI
            ReferenceNumber: "UPI-EXP-9921",
            PayeeName: "Office Mart Supplies",
            Notes: "Monthly stationery and printer paper"
        );

        var expRes = await _client.PostAsJsonAsync("/api/v1/expenses", expReq);
        expRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var expJson = await expRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>();
        expJson!.Success.Should().BeTrue();
        var exp = expJson.Data!;
        exp.Amount.Should().Be(2500m);
        exp.GstAmount.Should().Be(450m);
        exp.TotalAmount.Should().Be(2950m);
        exp.VoucherNumber.Should().StartWith("EXP/");

        // 3. List Expenses
        var expListRes = await _client.GetAsync("/api/v1/expenses");
        expListRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var expListJson = await expListRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ExpenseDto>>>();
        expListJson!.Success.Should().BeTrue();
        expListJson.Data.Should().Contain(e => e.Id == exp.Id);
    }
}

