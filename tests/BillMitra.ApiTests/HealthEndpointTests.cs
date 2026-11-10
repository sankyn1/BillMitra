using System.Net;
using System.Net.Http.Json;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.System;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SimpleHealthEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemHealthEndpoint_ShouldReturnStandardApiResponse()
    {
        var response = await _client.GetAsync("/api/v1/system/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<SystemHealthDto>>();

        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.AppName.Should().Be("BillMitra");
    }
}

