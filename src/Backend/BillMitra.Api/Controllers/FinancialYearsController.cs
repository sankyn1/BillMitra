using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.FinancialYear;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/financial-years")]
public class FinancialYearsController : ControllerBase
{
    private readonly IFinancialYearService _financialYearService;

    public FinancialYearsController(IFinancialYearService financialYearService)
    {
        _financialYearService = financialYearService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<FinancialYearDto>>> GetCurrent(CancellationToken cancellationToken)
    {
        var fy = await _financialYearService.GetActiveFinancialYearAsync(cancellationToken);
        if (fy == null)
        {
            return NotFound(ApiResponse<FinancialYearDto>.Fail("No active financial year found.", statusCode: 404));
        }
        return Ok(ApiResponse<FinancialYearDto>.Ok(fy));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FinancialYearDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _financialYearService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinancialYearDto>>.Ok(list));
    }
}
