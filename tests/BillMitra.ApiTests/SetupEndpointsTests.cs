using System.Net;
using System.Net.Http.Json;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class SetupEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SetupEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSetupStatus_ReturnsSuccessStatus()
    {
        var response = await _client.GetAsync("/api/v1/setup/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SetupStatusDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Initialize_WithInvalidGstin_ReturnsBadRequest()
    {
        var badRequest = new InitialSetupRequest
        {
            LegalName = "Test Company",
            StateCode = "24",
            Gstin = "INVALID_GSTIN",
            OwnerUsername = "admin",
            OwnerPassword = "password123"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/setup/initialize", badRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SetupStatusDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid GSTIN format");
    }
}

