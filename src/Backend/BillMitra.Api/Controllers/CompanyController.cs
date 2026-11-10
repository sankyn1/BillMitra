using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Company;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _companyService.GetProfileAsync(cancellationToken);
        if (profile == null)
        {
            return NotFound(ApiResponse<CompanyProfileDto>.Fail("Company profile has not been initialized.", statusCode: 404));
        }
        return Ok(ApiResponse<CompanyProfileDto>.Ok(profile));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto>>> UpdateProfile(
        [FromBody] UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _companyService.UpdateProfileAsync(request, cancellationToken);
            return Ok(ApiResponse<CompanyProfileDto>.Ok(updated, "Company profile updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CompanyProfileDto>.Fail(ex.Message));
        }
    }
}
