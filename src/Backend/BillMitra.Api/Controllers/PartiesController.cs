using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Common;
using BillMitra.Contracts.Masters;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PartiesController : ControllerBase
{
    private readonly IMasterService _masterService;

    public PartiesController(IMasterService masterService)
    {
        _masterService = masterService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PartyDto>>>> GetAll(
        [FromQuery] int? partyType,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var parties = await _masterService.GetPartiesAsync(partyType, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PartyDto>>.Ok(parties));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PartyDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var party = await _masterService.GetPartyByIdAsync(id, cancellationToken);
        if (party == null)
            return NotFound(ApiResponse<PartyDto>.Fail("Party was not found.", statusCode: 404));

        return Ok(ApiResponse<PartyDto>.Ok(party));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PartyDto>>> Create(
        [FromBody] CreateOrUpdatePartyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _masterService.CreatePartyAsync(request, cancellationToken);
            return Ok(ApiResponse<PartyDto>.Ok(created, "Party created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PartyDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PartyDto>>> Update(
        Guid id,
        [FromBody] CreateOrUpdatePartyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _masterService.UpdatePartyAsync(id, request, cancellationToken);
            return Ok(ApiResponse<PartyDto>.Ok(updated, "Party updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PartyDto>.Fail(ex.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<PartyDto>.Fail("Party was not found.", statusCode: 404));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _masterService.DeletePartyAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Party marked as inactive."));
    }
}

