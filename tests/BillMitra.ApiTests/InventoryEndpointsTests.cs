using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Inventory;
using BillMitra.Contracts.Masters;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class InventoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InventoryEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Warehouses_CreateAndList_Succeeds()
    {
        var code = $"WH{DateTime.UtcNow.Ticks % 10000}";
        var req = new CreateWarehouseRequest(
            Name: "GIDC Vatva Storage Yard",
            Code: code,
            Address: "Phase 4, Vatva GIDC",
            City: "Ahmedabad",
            StateCode: "24",
            IsDefault: false
        );

        var createRes = await _client.PostAsJsonAsync("/api/v1/warehouses", req);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        createJson!.Success.Should().BeTrue();
        var wh = createJson.Data!;
        wh.Name.Should().Be("GIDC Vatva Storage Yard");
        wh.Code.Should().Be(code);

        // List
        var listRes = await _client.GetAsync("/api/v1/warehouses");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<WarehouseDto>>>();
        listJson!.Success.Should().BeTrue();
        listJson.Data.Should().Contain(w => w.Id == wh.Id);
    }

    [Fact]
    public async Task Inventory_BatchesAndAdjustments_Succeeds()
    {
        // 1. Create a product first
        var prodCode = $"PRD-{DateTime.UtcNow.Ticks % 100000}";
        var prodReq = new CreateOrUpdateProductRequest
        {
            Name = "Industrial Sensor Module",
            ItemCode = prodCode,
            Description = "High-precision pressure sensor",
            ItemType = 1,
            CategoryId = null,
            HsnSacCode = "9026",
            UnitCode = "PCS",
            PurchasePrice = 1500m,
            SalePrice = 2200m,
            GstRatePercent = 18m,
            CessPercent = 0m,
            IsTaxInclusive = false,
            OpeningStock = 50m,
            MinStockAlert = 10m
        };

        var prodRes = await _client.PostAsJsonAsync("/api/v1/products", prodReq);
        prodRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var prodJson = await prodRes.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        var prod = prodJson!.Data!;

        // 2. Fetch default warehouse
        var whListRes = await _client.GetAsync("/api/v1/warehouses");
        var whListJson = await whListRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<WarehouseDto>>>();
        var wh = whListJson!.Data![0];

        // 3. Register a batch
        var batchReq = new CreateStockBatchRequest(
            ProductId: prod.Id,
            WarehouseId: wh.Id,
            BatchNumber: $"B-{DateTime.UtcNow.Ticks % 10000}",
            ManufacturingDate: DateTime.UtcNow.AddMonths(-2),
            ExpiryDate: DateTime.UtcNow.AddMonths(10),
            QuantityOnHand: 25m,
            PurchasePrice: 1500m,
            Mrp: 2500m
        );

        var batchRes = await _client.PostAsJsonAsync("/api/v1/inventory/batches", batchReq);
        batchRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchJson = await batchRes.Content.ReadFromJsonAsync<ApiResponse<StockItemBatchDto>>();
        batchJson!.Success.Should().BeTrue();
        var batch = batchJson.Data!;
        batch.QuantityOnHand.Should().Be(25m);

        // 4. Record Physical Count Adjustment
        var adjReq = new StockAdjustmentRequest(
            ProductId: prod.Id,
            WarehouseId: wh.Id,
            BatchId: batch.Id,
            Reason: 1, // PhysicalCountReconciliation
            PhysicalQuantity: 30m, // Reconciled from 25 to 30
            Remarks: "Stock count audit positive variance"
        );

        var adjRes = await _client.PostAsJsonAsync("/api/v1/inventory/adjustments", adjReq);
        adjRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var adjJson = await adjRes.Content.ReadFromJsonAsync<ApiResponse<StockAdjustmentDto>>();
        adjJson!.Success.Should().BeTrue();
        adjJson.Data!.PhysicalQuantity.Should().Be(30m);

        // 5. Query Dashboard Metrics
        var metricsRes = await _client.GetAsync("/api/v1/inventory/metrics");
        metricsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var metricsJson = await metricsRes.Content.ReadFromJsonAsync<ApiResponse<InventoryDashboardMetricsDto>>();
        metricsJson!.Success.Should().BeTrue();
        metricsJson.Data!.TotalActiveProducts.Should().BeGreaterThan(0);
    }
}
