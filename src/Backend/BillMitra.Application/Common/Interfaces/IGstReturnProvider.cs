using System;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public sealed record GstProviderAuthResult(
    bool IsSuccess,
    string Message,
    string? ErrorCode = null
);

public sealed record GstProviderSubmissionResult(
    bool IsSuccess,
    string? ReferenceId,
    string? Arn,
    DateTime? AckDate,
    string Message,
    string? RawResponseJson = null,
    string? ErrorCode = null
);

public sealed record GstProviderStatusResult(
    string Status,
    string? Arn,
    DateTime? ArnDate,
    string Message,
    string? RawResponseJson = null
);

public interface IGstReturnProvider
{
    string ProviderName { get; }
    Task<GstProviderAuthResult> ValidateRegistrationAsync(string gstin, CancellationToken ct = default);
    Task<GstProviderSubmissionResult> SubmitReturnAsync(GstReturnFiling filing, CancellationToken ct = default);
    Task<GstProviderStatusResult> CheckStatusAsync(string providerReference, CancellationToken ct = default);
}

