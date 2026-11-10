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

    [HttpPut("letterhead-settings")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto>>> UpdateLetterheadSettings(
        [FromBody] UpdateLetterheadSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _companyService.UpdateLetterheadSettingsAsync(request, cancellationToken);
            return Ok(ApiResponse<CompanyProfileDto>.Ok(updated, "Letterhead settings updated successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CompanyProfileDto>.Fail(ex.Message));
        }
    }

    [HttpPost("letterhead")]
    public async Task<ActionResult<ApiResponse<string>>> UploadLetterhead(
        [FromBody] UploadLetterheadRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Base64Data))
        {
            return BadRequest(ApiResponse<string>.Fail("Base64 image data is required."));
        }

        try
        {
            var path = await _companyService.SaveLetterheadImageAsync(request.Base64Data, request.FileName, cancellationToken);
            return Ok(ApiResponse<string>.Ok(path, "Letterhead graphic uploaded successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail($"Failed to upload letterhead: {ex.Message}"));
        }
    }

    [HttpDelete("letterhead")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveLetterhead(CancellationToken cancellationToken)
    {
        var success = await _companyService.RemoveLetterheadImageAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Ok(success, "Letterhead removed successfully."));
    }
}
