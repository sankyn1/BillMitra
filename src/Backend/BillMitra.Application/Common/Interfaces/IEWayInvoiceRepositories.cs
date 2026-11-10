using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public interface IEWayBillRepository
{
    Task<EWayBillRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EWayBillRecord?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<EWayBillRecord?> GetByEWayBillNumberAsync(string ewayBillNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EWayBillRecord>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(EWayBillRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(EWayBillRecord record, CancellationToken cancellationToken = default);
}

public interface IEInvoiceRepository
{
    Task<EInvoiceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EInvoiceRecord?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<EInvoiceRecord?> GetByIrnAsync(string irn, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EInvoiceRecord>> ListAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(EInvoiceRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(EInvoiceRecord record, CancellationToken cancellationToken = default);
}
