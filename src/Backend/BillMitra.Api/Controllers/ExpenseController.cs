using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Purchases;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

public record CreateExpenseCategoryPayload(
    string Name,
    string? Description
);

[ApiController]
[Route("api/v1/expenses")]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpenseController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var list = await _expenseService.GetCategoriesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ExpenseCategoryDto>>.Ok(list));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> CreateCategory(
        [FromBody] CreateExpenseCategoryPayload payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            return BadRequest(ApiResponse<ExpenseCategoryDto>.Fail("Category name is required."));
        }

        try
        {
            var result = await _expenseService.CreateCategoryAsync(payload.Name, payload.Description, cancellationToken);
            return Ok(ApiResponse<ExpenseCategoryDto>.Ok(result, "Category created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ExpenseCategoryDto>.Fail(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExpenseCategoryId == Guid.Empty)
        {
            return BadRequest(ApiResponse<ExpenseDto>.Fail("Expense Category ID is required."));
        }

        if (request.Amount <= 0)
        {
            return BadRequest(ApiResponse<ExpenseDto>.Fail("Expense amount must be greater than zero."));
        }

        try
        {
            var result = await _expenseService.CreateExpenseAsync(request, cancellationToken);
            return Ok(ApiResponse<ExpenseDto>.Ok(result, "Expense voucher recorded successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ExpenseDto>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenseDto>>>> GetExpenses(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var list = await _expenseService.ListAsync(fromDate, toDate, categoryId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ExpenseDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<ExpenseDto>.Fail("Expense not found."));
        }

        return Ok(ApiResponse<ExpenseDto>.Ok(result));
    }
}
