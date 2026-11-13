using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Setup;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SetupController : ControllerBase
{
    private readonly ISetupService _setupService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(ISetupService setupService, ILogger<SetupController> logger)
    {
        _setupService = setupService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<SetupStatusDto>>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _setupService.GetStatusAsync(cancellationToken);
        return Ok(ApiResponse<SetupStatusDto>.Ok(status));
    }

    [HttpPost("initialize")]
    public async Task<ActionResult<ApiResponse<SetupStatusDto>>> Initialize(
        [FromBody] InitialSetupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _setupService.CompleteSetupAsync(request, cancellationToken);
            return Ok(ApiResponse<SetupStatusDto>.Ok(result, "First-Run setup completed successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SetupStatusDto>.Fail(ex.Message));
        }
    }

    [HttpPost("reset-data")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetData(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _setupService.ResetTransactionalAndMasterDataAsync(cancellationToken);
            return Ok(ApiResponse<bool>.Ok(result, "All transaction and master records have been cleared and document sequence counters have been reset to 0."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear test data.");
            return BadRequest(ApiResponse<bool>.Fail($"Failed to reset data: {ex.Message}"));
        }
    }
}
