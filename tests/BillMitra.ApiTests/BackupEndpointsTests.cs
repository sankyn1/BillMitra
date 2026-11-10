using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Backups;
using BillMitra.Contracts.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class BackupEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BackupEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Backups_GetConfig_ReturnsSuccess()
    {
        var res = await _client.GetAsync("/api/v1/backups/config");
        var content = await res.Content.ReadAsStringAsync();
        if (res.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"HTTP ERROR: {content}");
        }
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadFromJsonAsync<ApiResponse<BackupConfigurationDto>>();
        json.Should().NotBeNull();
        json!.Success.Should().BeTrue();
        json.Data.Should().NotBeNull();
        json.Data.RetentionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Backups_CreateAndGetHistory_ReturnsSuccess()
    {
        var request = new CreateBackupRequest("Manual", "Integration test backup");
        var createRes = await _client.PostAsJsonAsync("/api/v1/backups", request);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createRes.Content.ReadFromJsonAsync<ApiResponse<BackupRecordDto>>();
        createJson.Should().NotBeNull();
        createJson!.Success.Should().BeTrue();
        createJson.Data.Should().NotBeNull();
        createJson.Data.Status.Should().Be("Completed");

        // Verify history endpoint
        var historyRes = await _client.GetAsync("/api/v1/backups");
        historyRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyJson = await historyRes.Content.ReadFromJsonAsync<ApiResponse<List<BackupRecordDto>>>();
        historyJson.Should().NotBeNull();
        historyJson!.Success.Should().BeTrue();
        historyJson.Data.Should().Contain(b => b.Id == createJson.Data.Id);

        // Verify integrity
        var verifyRes = await _client.PostAsync($"/api/v1/backups/{createJson.Data.Id}/verify", null);
        verifyRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
