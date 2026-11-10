using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Reports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BillMitra.ApiTests;

public class ReportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reports_FinancialOverview_ReturnsSuccess()
    {
        var res = await _client.GetAsync("/api/v1/reports/financial-overview");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadFromJsonAsync<ApiResponse<FinancialOverviewReportDto>>();
        json.Should().NotBeNull();
        json!.Success.Should().BeTrue();
        json.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Reports_GstSummary_ReturnsSuccess()
    {
        var res = await _client.GetAsync("/api/v1/reports/gst-summary");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadFromJsonAsync<ApiResponse<GstLiabilitySummaryDto>>();
        json.Should().NotBeNull();
        json!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Reports_AgingBuckets_ReturnsSuccess()
    {
        var res = await _client.GetAsync("/api/v1/reports/aging-buckets");
        var content = await res.Content.ReadAsStringAsync();
        if (res.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("HTTP ERROR: " + content);
        }
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadFromJsonAsync<ApiResponse<System.Collections.Generic.IReadOnlyList<AgingBucketDto>>>();
        json.Should().NotBeNull();
        json!.Success.Should().BeTrue();
        json.Data.Should().HaveCount(4); // 0-30, 31-60, 61-90, 90+
    }

    [Fact]
    public async Task Reports_ComprehensiveBundle_ReturnsSuccess()
    {
        var res = await _client.GetAsync("/api/v1/reports/comprehensive");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadFromJsonAsync<ApiResponse<ComprehensiveReportsBundleDto>>();
        json.Should().NotBeNull();
        json!.Success.Should().BeTrue();
        json.Data!.Overview.Should().NotBeNull();
        json.Data.GstSummary.Should().NotBeNull();
    }
}
