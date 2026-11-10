using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class EWayBillRepository : IEWayBillRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EWayBillRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> InsertAsync(EWayBillRecord record, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
INSERT INTO eway_bills (
    id, sales_document_id, eway_bill_number, eway_bill_date, valid_until, status,
    supply_type, sub_supply_type, doc_type, doc_number, doc_date,
    from_gstin, from_name, from_address, from_place, from_pincode, from_state_code,
    to_gstin, to_name, to_address, to_place, to_pincode, to_state_code,
    total_invoice_value, taxable_amount, cgst_amount, sgst_amount, igst_amount, cess_amount,
    transportation_mode, transporter_id, transporter_name, transport_doc_no, transport_doc_date,
    vehicle_no, vehicle_type, approximate_distance_km, cancel_reason, cancel_remarks, cancelled_at_utc,
    created_at_utc, updated_at_utc
) VALUES (
    @Id, @SalesDocumentId, @EWayBillNumber, @EWayBillDate, @ValidUntil, @Status,
    @SupplyType, @SubSupplyType, @DocType, @DocNumber, @DocDate,
    @FromGstin, @FromName, @FromAddress, @FromPlace, @FromPincode, @FromStateCode,
    @ToGstin, @ToName, @ToAddress, @ToPlace, @ToPincode, @ToStateCode,
    @TotalInvoiceValue, @TaxableAmount, @CgstAmount, @SgstAmount, @IgstAmount, @CessAmount,
    @TransportationMode, @TransporterId, @TransporterName, @TransportDocNo, @TransportDocDate,
    @VehicleNo, @VehicleType, @ApproximateDistanceKm, @CancelReason, @CancelRemarks, @CancelledAt,
    @CreatedAt, @UpdatedAt
);";

        await conn.ExecuteAsync(sql, new
        {
            record.Id,
            record.SalesDocumentId,
            record.EWayBillNumber,
            EWayBillDate = record.EWayBillDate.UtcDateTime,
            ValidUntil = record.ValidUntil.UtcDateTime,
            Status = (int)record.Status,
            SupplyType = (int)record.SupplyType,
            SubSupplyType = (int)record.SubSupplyType,
            record.DocType,
            record.DocNumber,
            record.DocDate,
            record.FromGstin,
            record.FromName,
            record.FromAddress,
            record.FromPlace,
            record.FromPincode,
            record.FromStateCode,
            record.ToGstin,
            record.ToName,
            record.ToAddress,
            record.ToPlace,
            record.ToPincode,
            record.ToStateCode,
            record.TotalInvoiceValue,
            record.TaxableAmount,
            record.CgstAmount,
            record.SgstAmount,
            record.IgstAmount,
            record.CessAmount,
            TransportationMode = (int)record.TransportationMode,
            record.TransporterId,
            record.TransporterName,
            record.TransportDocNo,
            record.TransportDocDate,
            record.VehicleNo,
            VehicleType = (int)record.VehicleType,
            record.ApproximateDistanceKm,
            record.CancelReason,
            record.CancelRemarks,
            CancelledAt = record.CancelledAt?.UtcDateTime,
            CreatedAt = record.CreatedAt.UtcDateTime,
            UpdatedAt = record.UpdatedAt?.UtcDateTime
        });

        return record.Id;
    }

    public async Task UpdateAsync(EWayBillRecord record, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
UPDATE eway_bills SET
    status = @Status,
    vehicle_no = @VehicleNo,
    vehicle_type = @VehicleType,
    transportation_mode = @TransportationMode,
    transporter_id = @TransporterId,
    transporter_name = @TransporterName,
    transport_doc_no = @TransportDocNo,
    transport_doc_date = @TransportDocDate,
    cancel_reason = @CancelReason,
    cancel_remarks = @CancelRemarks,
    cancelled_at_utc = @CancelledAt,
    updated_at_utc = @UpdatedAt
WHERE id = @Id;";

        await conn.ExecuteAsync(sql, new
        {
            record.Id,
            Status = (int)record.Status,
            record.VehicleNo,
            VehicleType = (int)record.VehicleType,
            TransportationMode = (int)record.TransportationMode,
            record.TransporterId,
            record.TransporterName,
            record.TransportDocNo,
            record.TransportDocDate,
            record.CancelReason,
            record.CancelRemarks,
            CancelledAt = record.CancelledAt?.UtcDateTime,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<EWayBillRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM eway_bills WHERE id = @Id;";
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row == null ? null : MapRow(row);
    }

    public async Task<EWayBillRecord?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT TOP 1 * FROM eway_bills WHERE sales_document_id = @DocumentId ORDER BY created_at_utc DESC;";
        // For Postgres/Sqlite fallback if TOP 1 fails:
        try
        {
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { DocumentId = documentId });
            return row == null ? null : MapRow(row);
        }
        catch
        {
            const string altSql = "SELECT * FROM eway_bills WHERE sales_document_id = @DocumentId ORDER BY created_at_utc DESC LIMIT 1;";
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(altSql, new { DocumentId = documentId });
            return row == null ? null : MapRow(row);
        }
    }

    public async Task<EWayBillRecord?> GetByEWayBillNumberAsync(string ewayBillNumber, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM eway_bills WHERE eway_bill_number = @EWayBillNumber;";
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { EWayBillNumber = ewayBillNumber });
        return row == null ? null : MapRow(row);
    }

    public async Task<IReadOnlyList<EWayBillRecord>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var offset = (page - 1) * pageSize;
        const string sql = @"
SELECT * FROM eway_bills
ORDER BY created_at_utc DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            var rows = await conn.QueryAsync<dynamic>(sql, new { Offset = offset, PageSize = pageSize });
            return rows.Select(MapRow).ToList();
        }
        catch
        {
            const string altSql = "SELECT * FROM eway_bills ORDER BY created_at_utc DESC LIMIT @PageSize OFFSET @Offset;";
            var rows = await conn.QueryAsync<dynamic>(altSql, new { Offset = offset, PageSize = pageSize });
            return rows.Select(MapRow).ToList();
        }
    }

    private static EWayBillRecord MapRow(dynamic row)
    {
        return new EWayBillRecord
        {
            Id = Guid.Parse(row.id.ToString()),
            SalesDocumentId = Guid.Parse(row.sales_document_id.ToString()),
            EWayBillNumber = row.eway_bill_number,
            EWayBillDate = DateTimeOffset.Parse(row.eway_bill_date.ToString()),
            ValidUntil = DateTimeOffset.Parse(row.valid_until.ToString()),
            Status = (EWayBillStatus)(int)row.status,
            SupplyType = (SupplyType)(int)row.supply_type,
            SubSupplyType = (EWayBillSubSupplyType)(int)row.sub_supply_type,
            DocType = row.doc_type,
            DocNumber = row.doc_number,
            DocDate = DateTime.Parse(row.doc_date.ToString()),
            FromGstin = row.from_gstin ?? string.Empty,
            FromName = row.from_name ?? string.Empty,
            FromAddress = row.from_address ?? string.Empty,
            FromPlace = row.from_place ?? string.Empty,
            FromPincode = row.from_pincode ?? string.Empty,
            FromStateCode = row.from_state_code ?? string.Empty,
            ToGstin = row.to_gstin,
            ToName = row.to_name ?? string.Empty,
            ToAddress = row.to_address ?? string.Empty,
            ToPlace = row.to_place ?? string.Empty,
            ToPincode = row.to_pincode ?? string.Empty,
            ToStateCode = row.to_state_code ?? string.Empty,
            TotalInvoiceValue = (decimal)row.total_invoice_value,
            TaxableAmount = (decimal)row.taxable_amount,
            CgstAmount = (decimal)row.cgst_amount,
            SgstAmount = (decimal)row.sgst_amount,
            IgstAmount = (decimal)row.igst_amount,
            CessAmount = (decimal)row.cess_amount,
            TransportationMode = (TransportationMode)(int)row.transportation_mode,
            TransporterId = row.transporter_id,
            TransporterName = row.transporter_name,
            TransportDocNo = row.transport_doc_no,
            TransportDocDate = row.transport_doc_date != null ? DateTime.Parse(row.transport_doc_date.ToString()) : null,
            VehicleNo = row.vehicle_no,
            VehicleType = (VehicleType)(int)row.vehicle_type,
            ApproximateDistanceKm = (int)row.approximate_distance_km,
            CancelReason = row.cancel_reason,
            CancelRemarks = row.cancel_remarks,
            CancelledAt = row.cancelled_at_utc != null ? DateTimeOffset.Parse(row.cancelled_at_utc.ToString()) : null,
            CreatedAt = DateTimeOffset.Parse(row.created_at_utc.ToString()),
            UpdatedAt = row.updated_at_utc != null ? DateTimeOffset.Parse(row.updated_at_utc.ToString()) : null
        };
    }
}

public class EInvoiceRepository : IEInvoiceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EInvoiceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> InsertAsync(EInvoiceRecord record, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
INSERT INTO einvoices (
    id, sales_document_id, irn, ack_no, ack_date, signed_invoice, signed_qr_code,
    status, cancel_reason, cancel_remarks, cancelled_at_utc, created_at_utc, updated_at_utc
) VALUES (
    @Id, @SalesDocumentId, @Irn, @AckNo, @AckDate, @SignedInvoice, @SignedQrCode,
    @Status, @CancelReason, @CancelRemarks, @CancelledAt, @CreatedAt, @UpdatedAt
);";

        await conn.ExecuteAsync(sql, new
        {
            record.Id,
            record.SalesDocumentId,
            record.Irn,
            record.AckNo,
            AckDate = record.AckDate.UtcDateTime,
            record.SignedInvoice,
            record.SignedQrCode,
            Status = (int)record.Status,
            CancelReason = (int?)record.CancelReason,
            record.CancelRemarks,
            CancelledAt = record.CancelledAt?.UtcDateTime,
            CreatedAt = record.CreatedAt.UtcDateTime,
            UpdatedAt = record.UpdatedAt?.UtcDateTime
        });

        return record.Id;
    }

    public async Task UpdateAsync(EInvoiceRecord record, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
UPDATE einvoices SET
    status = @Status,
    cancel_reason = @CancelReason,
    cancel_remarks = @CancelRemarks,
    cancelled_at_utc = @CancelledAt,
    updated_at_utc = @UpdatedAt
WHERE id = @Id;";

        await conn.ExecuteAsync(sql, new
        {
            record.Id,
            Status = (int)record.Status,
            CancelReason = (int?)record.CancelReason,
            record.CancelRemarks,
            CancelledAt = record.CancelledAt?.UtcDateTime,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<EInvoiceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM einvoices WHERE id = @Id;";
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row == null ? null : MapRow(row);
    }

    public async Task<EInvoiceRecord?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT TOP 1 * FROM einvoices WHERE sales_document_id = @DocumentId ORDER BY created_at_utc DESC;";
        try
        {
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { DocumentId = documentId });
            return row == null ? null : MapRow(row);
        }
        catch
        {
            const string altSql = "SELECT * FROM einvoices WHERE sales_document_id = @DocumentId ORDER BY created_at_utc DESC LIMIT 1;";
            var row = await conn.QuerySingleOrDefaultAsync<dynamic>(altSql, new { DocumentId = documentId });
            return row == null ? null : MapRow(row);
        }
    }

    public async Task<EInvoiceRecord?> GetByIrnAsync(string irn, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM einvoices WHERE irn = @Irn;";
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Irn = irn });
        return row == null ? null : MapRow(row);
    }

    public async Task<IReadOnlyList<EInvoiceRecord>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var offset = (page - 1) * pageSize;
        const string sql = @"
SELECT * FROM einvoices
ORDER BY created_at_utc DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            var rows = await conn.QueryAsync<dynamic>(sql, new { Offset = offset, PageSize = pageSize });
            return rows.Select(MapRow).ToList();
        }
        catch
        {
            const string altSql = "SELECT * FROM einvoices ORDER BY created_at_utc DESC LIMIT @PageSize OFFSET @Offset;";
            var rows = await conn.QueryAsync<dynamic>(altSql, new { Offset = offset, PageSize = pageSize });
            return rows.Select(MapRow).ToList();
        }
    }

    private static EInvoiceRecord MapRow(dynamic row)
    {
        return new EInvoiceRecord
        {
            Id = Guid.Parse(row.id.ToString()),
            SalesDocumentId = Guid.Parse(row.sales_document_id.ToString()),
            Irn = row.irn,
            AckNo = row.ack_no,
            AckDate = DateTimeOffset.Parse(row.ack_date.ToString()),
            SignedInvoice = row.signed_invoice ?? string.Empty,
            SignedQrCode = row.signed_qr_code ?? string.Empty,
            Status = (EInvoiceStatus)(int)row.status,
            CancelReason = row.cancel_reason != null ? (EInvoiceCancelReason)(int)row.cancel_reason : null,
            CancelRemarks = row.cancel_remarks,
            CancelledAt = row.cancelled_at_utc != null ? DateTimeOffset.Parse(row.cancelled_at_utc.ToString()) : null,
            CreatedAt = DateTimeOffset.Parse(row.created_at_utc.ToString()),
            UpdatedAt = row.updated_at_utc != null ? DateTimeOffset.Parse(row.updated_at_utc.ToString()) : null
        };
    }
}
