using BillMitra.Contracts.Masters;

namespace BillMitra.Application.Common.Interfaces;

public interface IMasterService
{
    // Products
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateOrUpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, CreateOrUpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> BulkImportProductsAsync(IReadOnlyList<ProductCsvImportRow> rows, CancellationToken cancellationToken = default);

    // Units & Categories
    Task<IReadOnlyList<UnitDto>> GetUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductCategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken cancellationToken = default);

    // Parties (Customers & Suppliers)
    Task<IReadOnlyList<PartyDto>> GetPartiesAsync(int? partyType = null, string? search = null, CancellationToken cancellationToken = default);
    Task<PartyDto?> GetPartyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PartyDto> CreatePartyAsync(CreateOrUpdatePartyRequest request, CancellationToken cancellationToken = default);
    Task<PartyDto> UpdatePartyAsync(Guid id, CreateOrUpdatePartyRequest request, CancellationToken cancellationToken = default);
    Task DeletePartyAsync(Guid id, CancellationToken cancellationToken = default);
}

