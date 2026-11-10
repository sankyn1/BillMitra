using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Masters;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMasterService _masterService;

    public ProductsController(IMasterService masterService)
    {
        _masterService = masterService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var products = await _masterService.GetProductsAsync(search, categoryId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductDto>>.Ok(products));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _masterService.GetProductByIdAsync(id, cancellationToken);
        if (product == null)
            return NotFound(ApiResponse<ProductDto>.Fail("Product was not found.", statusCode: 404));

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
        [FromBody] CreateOrUpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _masterService.CreateProductAsync(request, cancellationToken);
            return Ok(ApiResponse<ProductDto>.Ok(created, "Product created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
        Guid id,
        [FromBody] CreateOrUpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _masterService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(ApiResponse<ProductDto>.Ok(updated, "Product updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ProductDto>.Fail("Product was not found.", statusCode: 404));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _masterService.DeleteProductAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Product marked as inactive."));
    }

    [HttpGet("units")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnitDto>>>> GetUnits(CancellationToken cancellationToken)
    {
        var units = await _masterService.GetUnitsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UnitDto>>.Ok(units));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductCategoryDto>>>> GetCategories(CancellationToken cancellationToken)
    {
        var cats = await _masterService.GetCategoriesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductCategoryDto>>.Ok(cats));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<ProductCategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cat = await _masterService.CreateCategoryAsync(request.Name, request.Description, cancellationToken);
            return Ok(ApiResponse<ProductCategoryDto>.Ok(cat, "Category created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductCategoryDto>.Fail(ex.Message));
        }
    }

    [HttpPost("bulk-import")]
    public async Task<ActionResult<ApiResponse<int>>> BulkImport(
        [FromBody] List<ProductCsvImportRow> rows,
        CancellationToken cancellationToken)
    {
        var count = await _masterService.BulkImportProductsAsync(rows, cancellationToken);
        return Ok(ApiResponse<int>.Ok(count, $"Successfully imported {count} products."));
    }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
