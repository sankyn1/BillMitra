using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.System;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public class HealthController : ControllerBase
{
    private readonly ISystemHealthService _healthService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ISystemHealthService healthService, ILogger<HealthController> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<SystemHealthDto>>> GetHealth(CancellationToken cancellationToken)
    {
        var health = await _healthService.GetHealthStatusAsync(cancellationToken);
        return Ok(ApiResponse<SystemHealthDto>.Ok(health, "System health retrieved successfully."));
    }

    [HttpGet("/health")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetSimpleHealth()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("/health/ready")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        var health = await _healthService.GetHealthStatusAsync(cancellationToken);
        if (health.DatabaseConnected)
        {
            return Ok(new { status = "Ready", database = "Connected" });
        }

        return StatusCode(StatusCodes.Status200OK, new { status = "Degraded", database = "Disconnected", note = "Application running offline" });
    }
}

