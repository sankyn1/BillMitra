using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BillMitra.Infrastructure.Services;

public class SandboxEWayBillProvider : IEWayBillProvider
{
    private readonly ILogger<SandboxEWayBillProvider> _logger;

    public SandboxEWayBillProvider(ILogger<SandboxEWayBillProvider> logger)
    {
        _logger = logger;
    }

    public Task<EWayBillGenerateResult> GenerateEWayBillAsync(EWayBillRecord record, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Sandbox E-Way Bill for Document: {DocNumber}, GrandTotal: {Total}", record.DocNumber, record.TotalInvoiceValue);

        // Generate 12-digit E-Way Bill Number (State Code + Random 10 digits)
        var statePrefix = !string.IsNullOrWhiteSpace(record.FromStateCode) ? record.FromStateCode : "24";
        var randomDigits = RandomNumberGenerator.GetInt32(1000000000, 2147483647).ToString();
        var ewayBillNo = $"{statePrefix}{randomDigits}".Substring(0, 12);

        var now = DateTimeOffset.UtcNow;
        // Standard statutory validity: 1 day per 200 km (min 1 day)
        var validityDays = Math.Max(1, (int)Math.Ceiling(record.ApproximateDistanceKm / 200.0));
        var validUntil = now.AddDays(validityDays);

        return Task.FromResult(new EWayBillGenerateResult(
            Success: true,
            EWayBillNumber: ewayBillNo,
            EWayBillDate: now,
            ValidUntil: validUntil,
            ErrorMessage: null
        ));
    }

    public Task<bool> UpdateVehicleAsync(string ewayBillNumber, string vehicleNo, string fromPlace, string fromState, string reason, string? remarks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updated Vehicle for E-Way Bill {EwbNo} to {VehicleNo}", ewayBillNumber, vehicleNo);
        return Task.FromResult(true);
    }

    public Task<bool> CancelEWayBillAsync(string ewayBillNumber, string reasonCode, string remarks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelled E-Way Bill {EwbNo}. Reason: {Reason}", ewayBillNumber, reasonCode);
        return Task.FromResult(true);
    }
}

public class SandboxEInvoiceProvider : IEInvoiceProvider
{
    private readonly ILogger<SandboxEInvoiceProvider> _logger;

    public SandboxEInvoiceProvider(ILogger<SandboxEInvoiceProvider> logger)
    {
        _logger = logger;
    }

    public Task<EInvoiceGenerateResult> GenerateIrnAsync(EInvoiceGenerationContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Sandbox E-Invoice IRN for Document {DocNumber} (Seller: {Seller})", context.DocumentNumber, context.SellerGstin);

        // Deterministic 64-character SHA-256 IRN
        var rawIrnKey = $"{context.SellerGstin}|26-27|{context.DocumentType}|{context.DocumentNumber}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawIrnKey));
        var irn = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var ackNo = $"1226{RandomNumberGenerator.GetInt32(100000, 999999)}{RandomNumberGenerator.GetInt32(10000, 99999)}";
        var ackDate = DateTimeOffset.UtcNow;

        var qrData = new
        {
            SellerGSTIN = context.SellerGstin,
            BuyerGSTIN = context.BuyerGstin,
            DocNo = context.DocumentNumber,
            DocTyp = context.DocumentType == "TaxInvoice" ? "INV" : "CRN",
            DocDt = context.DocumentDate.ToString("dd/MM/yyyy"),
            TotInvVal = context.GrandTotal,
            ItemCnt = 1,
            MainHsnCode = "8504",
            Irn = irn,
            AckNo = ackNo,
            AckDt = ackDate.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var signedQrString = JsonSerializer.Serialize(qrData);

        return Task.FromResult(new EInvoiceGenerateResult(
            Success: true,
            Irn: irn,
            AckNo: ackNo,
            AckDate: ackDate,
            SignedInvoice: Convert.ToBase64String(Encoding.UTF8.GetBytes(signedQrString)),
            SignedQrCode: signedQrString,
            ErrorMessage: null
        ));
    }

    public Task<bool> CancelIrnAsync(string irn, int cancelReason, string remarks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelled E-Invoice IRN {Irn}. Reason: {Reason}", irn, cancelReason);
        return Task.FromResult(true);
    }
}
