using BillMitra.Domain.Entities;

namespace BillMitra.Application.Common.Interfaces;

public interface ICompanyRepository
{
    Task<CompanyProfile?> GetPrimaryCompanyAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
    Task<CompanyProfile> CreateAsync(CompanyProfile company, CancellationToken cancellationToken = default);
    Task<CompanyProfile> UpdateAsync(CompanyProfile company, CancellationToken cancellationToken = default);
}

public interface IFinancialYearRepository
{
    Task<FinancialYear?> GetActiveFinancialYearAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialYear>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinancialYear> CreateAsync(FinancialYear financialYear, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> HasUsersAsync(CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, string[] roleNames, CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(string? search = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByCodeAsync(string itemCode, CancellationToken cancellationToken = default);
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitOfMeasure>> GetUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductCategory> CreateCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default);
}

public interface IPartyRepository
{
    Task<IReadOnlyList<Party>> GetAllAsync(int? partyType = null, string? search = null, CancellationToken cancellationToken = default);
    Task<Party?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Party> CreateAsync(Party party, CancellationToken cancellationToken = default);
    Task<Party> UpdateAsync(Party party, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISalesDocumentRepository
{
    Task<SalesDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesDocument>> GetAllAsync(int? documentType = null, int? status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? partyId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<SalesDocument> InsertAsync(SalesDocument doc, CancellationToken cancellationToken = default);
    Task<SalesDocument> UpdateAsync(SalesDocument doc, CancellationToken cancellationToken = default);
    Task<SalesDocument?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IDocumentSequenceRepository
{
    Task<DocumentSequence> GetOrCreateSequenceAsync(Guid financialYearId, BillMitra.Domain.Enums.DocumentType documentType, CancellationToken cancellationToken = default);
    Task<string> AllocateNextNumberAsync(Guid financialYearId, BillMitra.Domain.Enums.DocumentType documentType, CancellationToken cancellationToken = default);
}
