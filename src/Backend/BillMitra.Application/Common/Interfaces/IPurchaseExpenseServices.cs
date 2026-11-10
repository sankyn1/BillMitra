using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Purchases;

namespace BillMitra.Application.Common.Interfaces;

public interface IPurchaseService
{
    Task<PurchaseDocumentDto> CreateDraftAsync(CreateOrUpdatePurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseDocumentDto> UpdateDraftAsync(Guid id, CreateOrUpdatePurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseDocumentDto> FinalizeAsync(Guid id, FinalizePurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseDocumentDto> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<PurchaseDocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseDocumentDto>> ListAsync(int? documentType = null, int? status = null, Guid? supplierId = null, CancellationToken cancellationToken = default);
}

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ExpenseCategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseDto>> ListAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
