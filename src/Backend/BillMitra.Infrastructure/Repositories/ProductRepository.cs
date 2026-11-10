using System.Data;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Domain.Entities;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? search = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT id, item_code AS ItemCode, name AS Name, description AS Description,
                   item_type AS ItemType, category_id AS CategoryId, hsn_sac_code AS HsnSacCode,
                   unit_code AS UnitCode, purchase_price AS PurchasePrice, sale_price AS SalePrice,
                   gst_rate_percent AS GstRatePercent, cess_percent AS CessPercent,
                   is_tax_inclusive AS IsTaxInclusive, opening_stock AS OpeningStock,
                   current_stock AS CurrentStock, min_stock_alert AS MinStockAlert,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM products
            WHERE is_active = 1";

        if (categoryId.HasValue)
        {
            sql += " AND category_id = @CategoryId";
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (LOWER(name) LIKE @Search OR LOWER(item_code) LIKE @Search OR LOWER(hsn_sac_code) LIKE @Search)";
        }
        sql += " ORDER BY name ASC";

        var list = await connection.QueryAsync<Product>(sql, new
        {
            CategoryId = categoryId,
            Search = $"%{search?.Trim().ToLowerInvariant()}%"
        });

        return list.AsList();
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, item_code AS ItemCode, name AS Name, description AS Description,
                   item_type AS ItemType, category_id AS CategoryId, hsn_sac_code AS HsnSacCode,
                   unit_code AS UnitCode, purchase_price AS PurchasePrice, sale_price AS SalePrice,
                   gst_rate_percent AS GstRatePercent, cess_percent AS CessPercent,
                   is_tax_inclusive AS IsTaxInclusive, opening_stock AS OpeningStock,
                   current_stock AS CurrentStock, min_stock_alert AS MinStockAlert,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM products
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<Product?> GetByCodeAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, item_code AS ItemCode, name AS Name, description AS Description,
                   item_type AS ItemType, category_id AS CategoryId, hsn_sac_code AS HsnSacCode,
                   unit_code AS UnitCode, purchase_price AS PurchasePrice, sale_price AS SalePrice,
                   gst_rate_percent AS GstRatePercent, cess_percent AS CessPercent,
                   is_tax_inclusive AS IsTaxInclusive, opening_stock AS OpeningStock,
                   current_stock AS CurrentStock, min_stock_alert AS MinStockAlert,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM products
            WHERE LOWER(item_code) = LOWER(@ItemCode)";

        return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ItemCode = itemCode });
    }

    public async Task<Product> CreateAsync(Product p, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO products (
                id, item_code, name, description, item_type, category_id, hsn_sac_code,
                unit_code, purchase_price, sale_price, gst_rate_percent, cess_percent,
                is_tax_inclusive, opening_stock, current_stock, min_stock_alert,
                is_active, created_at_utc, updated_at_utc
            ) VALUES (
                @Id, @ItemCode, @Name, @Description, @ItemType, @CategoryId, @HsnSacCode,
                @UnitCode, @PurchasePrice, @SalePrice, @GstRatePercent, @CessPercent,
                @IsTaxInclusive, @OpeningStock, @CurrentStock, @MinStockAlert,
                @IsActive, @CreatedAtUtc, @UpdatedAtUtc
            )";

        await connection.ExecuteAsync(sql, p);
        return p;
    }

    public async Task<Product> UpdateAsync(Product p, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE products SET
                item_code = @ItemCode,
                name = @Name,
                description = @Description,
                item_type = @ItemType,
                category_id = @CategoryId,
                hsn_sac_code = @HsnSacCode,
                unit_code = @UnitCode,
                purchase_price = @PurchasePrice,
                sale_price = @SalePrice,
                gst_rate_percent = @GstRatePercent,
                cess_percent = @CessPercent,
                is_tax_inclusive = @IsTaxInclusive,
                min_stock_alert = @MinStockAlert,
                updated_at_utc = @UpdatedAtUtc
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, p);
        return p;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "UPDATE products SET is_active = 0, updated_at_utc = @Now WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id, Now = DateTime.UtcNow });
    }

    public async Task<IReadOnlyList<UnitOfMeasure>> GetUnitsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, code, name, is_active AS IsActive FROM units ORDER BY code ASC";
        var list = await connection.QueryAsync<UnitOfMeasure>(sql);
        return list.AsList();
    }

    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT id, name, description, is_active AS IsActive FROM product_categories ORDER BY name ASC";
        var list = await connection.QueryAsync<ProductCategory>(sql);
        return list.AsList();
    }

    public async Task<ProductCategory> CreateCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "INSERT INTO product_categories (id, name, description, is_active) VALUES (@Id, @Name, @Description, @IsActive)";
        await connection.ExecuteAsync(sql, category);
        return category;
    }
}
