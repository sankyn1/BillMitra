using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BillMitra.Infrastructure.Services;

public class SandboxGstReturnProvider : IGstReturnProvider
{
    private readonly ILogger<SandboxGstReturnProvider> _logger;

    public string ProviderName => "GSTN-Sandbox-Simulator";

    public SandboxGstReturnProvider(ILogger<SandboxGstReturnProvider> logger)
    {
        _logger = logger;
    }

    public Task<GstProviderAuthResult> ValidateRegistrationAsync(string gstin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gstin) || gstin.Length != 15)
        {
            return Task.FromResult(new GstProviderAuthResult(false, "Invalid GSTIN length or format.", "GSTN_INVALID_FORMAT"));
        }

        return Task.FromResult(new GstProviderAuthResult(true, $"GSTIN {gstin} is active and verified in sandbox registry."));
    }

    public Task<GstProviderSubmissionResult> SubmitReturnAsync(GstReturnFiling filing, CancellationToken ct = default)
    {
        _logger.LogInformation("Submitting {ReturnType} return for period {Period} to sandbox GSP", filing.ReturnType, filing.TaxPeriod);

        var now = DateTime.UtcNow;
        var refId = $"GSP-REF-{now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
        // Standard GSTN 15-character ARN format: e.g. AA2404261234567
        var stateDigits = filing.TaxPeriod.Length >= 2 ? filing.TaxPeriod.Substring(0, 2) : "24";
        var arn = $"AA{stateDigits}{now:MM}{now:yy}{Random.Shared.Next(1000000, 9999999)}";

        var responsePayload = new
        {
            status_cd = "1",
            status_desc = "SUCCESS",
            trans_id = refId,
            arn = arn,
            arn_date = now.ToString("yyyy-MM-dd HH:mm:ss"),
            return_type = filing.ReturnType.ToString(),
            period = filing.TaxPeriod
        };

        var json = JsonSerializer.Serialize(responsePayload);

        return Task.FromResult(new GstProviderSubmissionResult(
            IsSuccess: true,
            ReferenceId: refId,
            Arn: arn,
            AckDate: now,
            Message: $"Return submitted to GSTN sandbox. Application Reference Number (ARN): {arn}",
            RawResponseJson: json
        ));
    }

    public Task<GstProviderStatusResult> CheckStatusAsync(string providerReference, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return Task.FromResult(new GstProviderStatusResult(
            Status: "FILED",
            Arn: $"AA24{now:MM}{now:yy}{Random.Shared.Next(1000000, 9999999)}",
            ArnDate: now,
            Message: "Return filing acknowledged by GSTN system.",
            RawResponseJson: "{\"status\": \"FILED\", \"code\": \"PROCESSED\"}"
        ));
    }
}

