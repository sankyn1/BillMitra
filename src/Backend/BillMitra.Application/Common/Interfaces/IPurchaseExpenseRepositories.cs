using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public interface IPurchaseRepository
{
    Task<PurchaseDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseDocument>> ListAsync(int? documentType = null, int? status = null, Guid? supplierId = null, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(PurchaseDocument doc, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseDocument doc, CancellationToken cancellationToken = default);
    Task<decimal> GetSupplierOutstandingAsync(Guid supplierId, CancellationToken cancellationToken = default);
}

public interface IExpenseRepository
{
    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ExpenseCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseCategory> CreateCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseEntry>> ListAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<ExpenseEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(ExpenseEntry expense, CancellationToken cancellationToken = default);
    Task<string> GetNextVoucherNumberAsync(CancellationToken cancellationToken = default);
}
