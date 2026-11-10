using System.Net;
using System.Net.Http.Json;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Masters;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class MastersEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MastersEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
    {
        var request = new CreateOrUpdateProductRequest
        {
            ItemCode = $"TST-{DateTime.UtcNow.Ticks % 100000}",
            Name = "Organic Green Tea 250g",
            HsnSacCode = "0902",
            UnitCode = "BOX",
            SalePrice = 180.00m,
            PurchasePrice = 120.00m,
            GstRatePercent = 5.0m,
            OpeningStock = 25
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Organic Green Tea 250g");
        result.Data.GstRatePercent.Should().Be(5.0m);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidGstRate_ReturnsBadRequest()
    {
        var request = new CreateOrUpdateProductRequest
        {
            Name = "Invalid Tax Item",
            GstRatePercent = 9.99m // Invalid rate
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetParties_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/v1/parties");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PartyDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }
}

