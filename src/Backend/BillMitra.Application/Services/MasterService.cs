using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Masters;
using BillMitra.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class MasterService : IMasterService
{
    private readonly IProductRepository _productRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly ILogger<MasterService> _logger;

    public MasterService(
        IProductRepository productRepository,
        IPartyRepository partyRepository,
        ILogger<MasterService> logger)
    {
        _productRepository = productRepository;
        _partyRepository = partyRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var list = await _productRepository.GetAllAsync(search, categoryId, cancellationToken);
        var categories = (await _productRepository.GetCategoriesAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        return list.Select(p => MapToDto(p, categories.TryGetValue(p.CategoryId ?? Guid.Empty, out var catName) ? catName : null)).ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null) return null;

        string? categoryName = null;
        if (product.CategoryId.HasValue)
        {
            var cats = await _productRepository.GetCategoriesAsync(cancellationToken);
            categoryName = cats.FirstOrDefault(c => c.Id == product.CategoryId.Value)?.Name;
        }

        return MapToDto(product, categoryName);
    }

    public async Task<ProductDto> CreateProductAsync(CreateOrUpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product / Service name is required.", nameof(request.Name));

        if (!Product.ValidGstRates.Contains(request.GstRatePercent))
            throw new ArgumentException($"Invalid GST Rate {request.GstRatePercent}%. Allowed: 0, 0.1, 0.25, 1.5, 3, 5, 12, 18, 28", nameof(request.GstRatePercent));

        if (!string.IsNullOrWhiteSpace(request.HsnSacCode) && !Product.IsValidHsn(request.HsnSacCode))
            throw new ArgumentException("HSN/SAC code must be 2, 4, 6, or 8 digits.", nameof(request.HsnSacCode));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ItemCode = string.IsNullOrWhiteSpace(request.ItemCode) ? $"ITEM-{DateTime.UtcNow.Ticks % 1000000:D6}" : request.ItemCode.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ItemType = request.ItemType,
            CategoryId = request.CategoryId,
            HsnSacCode = request.HsnSacCode?.Trim() ?? string.Empty,
            UnitCode = string.IsNullOrWhiteSpace(request.UnitCode) ? "PCS" : request.UnitCode.Trim().ToUpperInvariant(),
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            GstRatePercent = request.GstRatePercent,
            CessPercent = request.CessPercent,
            IsTaxInclusive = request.IsTaxInclusive,
            OpeningStock = request.OpeningStock,
            CurrentStock = request.OpeningStock,
            MinStockAlert = request.MinStockAlert,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _productRepository.CreateAsync(product, cancellationToken);
        _logger.LogInformation("Created master product: {ItemCode} - {Name}", product.ItemCode, product.Name);

        return MapToDto(product, null);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, CreateOrUpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} was not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product / Service name is required.", nameof(request.Name));

        if (!Product.ValidGstRates.Contains(request.GstRatePercent))
            throw new ArgumentException($"Invalid GST Rate {request.GstRatePercent}%.", nameof(request.GstRatePercent));

        product.ItemCode = string.IsNullOrWhiteSpace(request.ItemCode) ? product.ItemCode : request.ItemCode.Trim();
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.ItemType = request.ItemType;
        product.CategoryId = request.CategoryId;
        product.HsnSacCode = request.HsnSacCode?.Trim() ?? string.Empty;
        product.UnitCode = string.IsNullOrWhiteSpace(request.UnitCode) ? "PCS" : request.UnitCode.Trim().ToUpperInvariant();
        product.PurchasePrice = request.PurchasePrice;
        product.SalePrice = request.SalePrice;
        product.GstRatePercent = request.GstRatePercent;
        product.CessPercent = request.CessPercent;
        product.IsTaxInclusive = request.IsTaxInclusive;
        product.MinStockAlert = request.MinStockAlert;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);
        return MapToDto(product, null);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _productRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<int> BulkImportProductsAsync(IReadOnlyList<ProductCsvImportRow> rows, CancellationToken cancellationToken = default)
    {
        int count = 0;
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Name)) continue;

            var product = new Product
            {
                Id = Guid.NewGuid(),
                ItemCode = string.IsNullOrWhiteSpace(r.ItemCode) ? $"IMP-{DateTime.UtcNow.Ticks % 1000000:D6}" : r.ItemCode.Trim(),
                Name = r.Name.Trim(),
                HsnSacCode = r.HsnSacCode?.Trim() ?? string.Empty,
                UnitCode = string.IsNullOrWhiteSpace(r.UnitCode) ? "PCS" : r.UnitCode.Trim().ToUpperInvariant(),
                SalePrice = r.SalePrice,
                PurchasePrice = r.PurchasePrice,
                GstRatePercent = Product.ValidGstRates.Contains(r.GstRatePercent) ? r.GstRatePercent : 18.0m,
                OpeningStock = r.OpeningStock,
                CurrentStock = r.OpeningStock,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _productRepository.CreateAsync(product, cancellationToken);
            count++;
        }
        return count;
    }

    public async Task<IReadOnlyList<UnitDto>> GetUnitsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _productRepository.GetUnitsAsync(cancellationToken);
        return list.Select(u => new UnitDto { Id = u.Id, Code = u.Code, Name = u.Name, IsActive = u.IsActive }).ToList();
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _productRepository.GetCategoriesAsync(cancellationToken);
        return list.Select(c => new ProductCategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive }).ToList();
    }

    public async Task<ProductCategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        var entity = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };

        var created = await _productRepository.CreateCategoryAsync(entity, cancellationToken);
        return new ProductCategoryDto { Id = created.Id, Name = created.Name, Description = created.Description, IsActive = created.IsActive };
    }

    // Party Management
    public async Task<IReadOnlyList<PartyDto>> GetPartiesAsync(int? partyType = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var list = await _partyRepository.GetAllAsync(partyType, search, cancellationToken);
        return list.Select(MapPartyToDto).ToList();
    }

    public async Task<PartyDto?> GetPartyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var party = await _partyRepository.GetByIdAsync(id, cancellationToken);
        return party == null ? null : MapPartyToDto(party);
    }

    public async Task<PartyDto> CreatePartyAsync(CreateOrUpdatePartyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Customer / Supplier name is required.", nameof(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Gstin))
        {
            if (!CompanyProfile.IsValidGstin(request.Gstin))
                throw new ArgumentException("Invalid GSTIN format for party.", nameof(request.Gstin));
        }

        var party = new Party
        {
            Id = Guid.NewGuid(),
            PartyType = request.PartyType,
            Name = request.Name.Trim(),
            TradeName = request.TradeName?.Trim(),
            Gstin = string.IsNullOrWhiteSpace(request.Gstin) ? null : request.Gstin.Trim().ToUpperInvariant(),
            Pan = !string.IsNullOrWhiteSpace(request.Gstin) ? CompanyProfile.ExtractPanFromGstin(request.Gstin) : null,
            GstRegistrationType = request.GstRegistrationType,
            StateCode = request.StateCode?.Trim() ?? string.Empty,
            StateName = request.StateName?.Trim() ?? string.Empty,
            AddressLine1 = request.AddressLine1?.Trim() ?? string.Empty,
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City?.Trim() ?? string.Empty,
            Pincode = request.Pincode?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Email = request.Email?.Trim(),
            CreditLimit = request.CreditLimit,
            CurrentBalance = 0,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _partyRepository.CreateAsync(party, cancellationToken);
        return MapPartyToDto(party);
    }

    public async Task<PartyDto> UpdatePartyAsync(Guid id, CreateOrUpdatePartyRequest request, CancellationToken cancellationToken = default)
    {
        var party = await _partyRepository.GetByIdAsync(id, cancellationToken);
        if (party == null)
            throw new KeyNotFoundException($"Party with ID {id} was not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Customer / Supplier name is required.", nameof(request.Name));

        party.PartyType = request.PartyType;
        party.Name = request.Name.Trim();
        party.TradeName = request.TradeName?.Trim();
        party.Gstin = string.IsNullOrWhiteSpace(request.Gstin) ? null : request.Gstin.Trim().ToUpperInvariant();
        party.Pan = !string.IsNullOrWhiteSpace(request.Gstin) ? CompanyProfile.ExtractPanFromGstin(request.Gstin) : null;
        party.GstRegistrationType = request.GstRegistrationType;
        party.StateCode = request.StateCode?.Trim() ?? string.Empty;
        party.StateName = request.StateName?.Trim() ?? string.Empty;
        party.AddressLine1 = request.AddressLine1?.Trim() ?? string.Empty;
        party.AddressLine2 = request.AddressLine2?.Trim();
        party.City = request.City?.Trim() ?? string.Empty;
        party.Pincode = request.Pincode?.Trim() ?? string.Empty;
        party.Phone = request.Phone?.Trim() ?? string.Empty;
        party.Email = request.Email?.Trim();
        party.CreditLimit = request.CreditLimit;
        party.UpdatedAtUtc = DateTime.UtcNow;

        await _partyRepository.UpdateAsync(party, cancellationToken);
        return MapPartyToDto(party);
    }

    public async Task DeletePartyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _partyRepository.DeleteAsync(id, cancellationToken);
    }

    private static ProductDto MapToDto(Product p, string? catName) => new()
    {
        Id = p.Id,
        ItemCode = p.ItemCode,
        Name = p.Name,
        Description = p.Description,
        ItemType = p.ItemType,
        CategoryId = p.CategoryId,
        CategoryName = catName,
        HsnSacCode = p.HsnSacCode,
        UnitCode = p.UnitCode,
        PurchasePrice = p.PurchasePrice,
        SalePrice = p.SalePrice,
        GstRatePercent = p.GstRatePercent,
        CessPercent = p.CessPercent,
        IsTaxInclusive = p.IsTaxInclusive,
        OpeningStock = p.OpeningStock,
        CurrentStock = p.CurrentStock,
        MinStockAlert = p.MinStockAlert,
        IsActive = p.IsActive,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc
    };

    private static PartyDto MapPartyToDto(Party p) => new()
    {
        Id = p.Id,
        PartyType = p.PartyType,
        Name = p.Name,
        TradeName = p.TradeName,
        Gstin = p.Gstin,
        Pan = p.Pan,
        GstRegistrationType = p.GstRegistrationType,
        StateCode = p.StateCode,
        StateName = p.StateName,
        AddressLine1 = p.AddressLine1,
        AddressLine2 = p.AddressLine2,
        City = p.City,
        Pincode = p.Pincode,
        Phone = p.Phone,
        Email = p.Email,
        CreditLimit = p.CreditLimit,
        CurrentBalance = p.CurrentBalance,
        IsActive = p.IsActive,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc
    };
}

