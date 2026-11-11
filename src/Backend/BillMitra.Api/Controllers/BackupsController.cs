using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Backups;
using BillMitra.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace BillMitra.Api.Controllers;

[ApiController]
[Route("api/v1/backups")]
public class BackupsController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupsController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BackupRecordDto>>> CreateBackup(
        [FromBody] CreateBackupRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _backupService.CreateBackupAsync(request, cancellationToken);
        return Ok(ApiResponse<BackupRecordDto>.Ok(record, "Backup created successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BackupRecordDto>>>> GetHistory(CancellationToken cancellationToken)
    {
        var history = await _backupService.GetBackupHistoryAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BackupRecordDto>>.Ok(history));
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyIntegrity(
        Guid id,
        CancellationToken cancellationToken)
    {
        var valid = await _backupService.VerifyBackupIntegrityAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(valid, valid ? "Backup verified and integrity check passed." : "Backup file integrity check failed."));
    }

    [HttpPost("restore")]
    public async Task<ActionResult<ApiResponse<RestoreBackupResultDto>>> Restore(
        [FromBody] RestoreBackupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _backupService.RestoreFromBackupAsync(request, cancellationToken);
        return Ok(ApiResponse<RestoreBackupResultDto>.Ok(result, result.Message));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBackup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _backupService.DeleteBackupAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(deleted, deleted ? "Backup deleted." : "Backup not found."));
    }

    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<BackupConfigurationDto>>> GetConfig(CancellationToken cancellationToken)
    {
        var config = await _backupService.GetConfigurationAsync(cancellationToken);
        return Ok(ApiResponse<BackupConfigurationDto>.Ok(config));
    }

    [HttpPut("config")]
    public async Task<ActionResult<ApiResponse<BackupConfigurationDto>>> UpdateConfig(
        [FromBody] UpdateBackupConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _backupService.UpdateConfigurationAsync(request, cancellationToken);
        return Ok(ApiResponse<BackupConfigurationDto>.Ok(updated, "Backup configuration saved."));
    }
}

