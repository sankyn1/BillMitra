using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Backups;
using Microsoft.Extensions.Logging;

namespace BillMitra.Application.Services;

public class BackupService : IBackupService
{
    private readonly IBackupRepository _backupRepository;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupRepository backupRepository,
        ILogger<BackupService> logger)
    {
        _backupRepository = backupRepository;
        _logger = logger;
    }

    public async Task<BackupRecordDto> CreateBackupAsync(CreateBackupRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _backupRepository.GetConfigurationAsync(cancellationToken);
        var folder = !string.IsNullOrWhiteSpace(config.BackupFolderPath)
            ? config.BackupFolderPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillMitra", "Backups");

        Directory.CreateDirectory(folder);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"BillMitra_Backup_{timestamp}.db";
        var filePath = Path.Combine(folder, fileName);

        _logger.LogInformation("Creating backup to {FilePath}", filePath);

        try
        {
            await _backupRepository.ExecutePhysicalBackupAsync(filePath, cancellationToken);

            var fileInfo = new FileInfo(filePath);
            var size = fileInfo.Exists ? fileInfo.Length : 0;
            string? checksum = null;

            if (fileInfo.Exists && size > 0)
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
                checksum = Convert.ToHexString(hash);
            }

            var record = new BackupRecordDto(
                Id: Guid.NewGuid(),
                FileName: fileName,
                FilePath: filePath,
                FileSizeBytes: size,
                BackupType: request.BackupType ?? "Manual",
                Status: "Completed",
                CreatedAtUtc: DateTime.UtcNow,
                Checksum: checksum,
                Notes: request.Notes
            );

            await _backupRepository.InsertRecordAsync(record, cancellationToken);
            await _backupRepository.UpdateConfigurationAsync(config with { LastBackupAtUtc = DateTime.UtcNow }, cancellationToken);

            // Execute retention pruning
            await PruneOldBackupsAsync(config.RetentionCount, cancellationToken);

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup at {FilePath}", filePath);
            var failedRecord = new BackupRecordDto(
                Id: Guid.NewGuid(),
                FileName: fileName,
                FilePath: filePath,
                FileSizeBytes: 0,
                BackupType: request.BackupType ?? "Manual",
                Status: "Failed",
                CreatedAtUtc: DateTime.UtcNow,
                Checksum: null,
                Notes: $"Error: {ex.Message}"
            );
            await _backupRepository.InsertRecordAsync(failedRecord, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<BackupRecordDto>> GetBackupHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _backupRepository.GetAllRecordsAsync(cancellationToken);
    }

    public async Task<bool> VerifyBackupIntegrityAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        var record = await _backupRepository.GetRecordByIdAsync(backupId, cancellationToken);
        if (record == null || !File.Exists(record.FilePath))
        {
            return false;
        }

        try
        {
            var isValid = await _backupRepository.VerifyPhysicalIntegrityAsync(record.FilePath, cancellationToken);
            var status = isValid ? "Verified" : "Failed";
            await _backupRepository.UpdateStatusAsync(backupId, status, record.Checksum, cancellationToken);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Integrity verification failed for backup {BackupId}", backupId);
            await _backupRepository.UpdateStatusAsync(backupId, "Failed", record.Checksum, cancellationToken);
            return false;
        }
    }

    public async Task<RestoreBackupResultDto> RestoreFromBackupAsync(RestoreBackupRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _backupRepository.GetRecordByIdAsync(request.BackupId, cancellationToken);
        if (record == null)
        {
            throw new InvalidOperationException($"Backup record with ID '{request.BackupId}' was not found.");
        }

        if (!File.Exists(record.FilePath))
        {
            throw new FileNotFoundException($"The physical backup file at '{record.FilePath}' does not exist.");
        }

        string? safetySnapshotFileName = null;
        if (request.ConfirmSafetySnapshot)
        {
            try
            {
                var safety = await CreateBackupAsync(new CreateBackupRequest("PreRestoreSnapshot", $"Pre-restore safety snapshot before restoring {record.FileName}"), cancellationToken);
                safetySnapshotFileName = safety.FileName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not take pre-restore safety snapshot.");
            }
        }

        await _backupRepository.RestorePhysicalDatabaseAsync(record.FilePath, cancellationToken);

        return new RestoreBackupResultDto(
            Success: true,
            Message: $"Database restored successfully from {record.FileName}.",
            SafetySnapshotFileName: safetySnapshotFileName,
            RestoredAtUtc: DateTime.UtcNow
        );
    }

    public async Task<bool> DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        var record = await _backupRepository.GetRecordByIdAsync(backupId, cancellationToken);
        if (record != null && File.Exists(record.FilePath))
        {
            try
            {
                File.Delete(record.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical backup file at {FilePath}", record.FilePath);
            }
        }

        return await _backupRepository.DeleteRecordAsync(backupId, cancellationToken);
    }

    public async Task<BackupConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _backupRepository.GetConfigurationAsync(cancellationToken);
    }

    public async Task<BackupConfigurationDto> UpdateConfigurationAsync(UpdateBackupConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _backupRepository.GetConfigurationAsync(cancellationToken);
        var updated = existing with
        {
            AutoBackupOnStartup = request.AutoBackupOnStartup,
            BackupFolderPath = !string.IsNullOrWhiteSpace(request.BackupFolderPath) ? request.BackupFolderPath : existing.BackupFolderPath,
            RetentionCount = request.RetentionCount > 0 ? request.RetentionCount : 10
        };

        await _backupRepository.UpdateConfigurationAsync(updated, cancellationToken);
        return updated;
    }

    public async Task ExecuteStartupAutoBackupIfNeededAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _backupRepository.GetConfigurationAsync(cancellationToken);
            if (!config.AutoBackupOnStartup)
            {
                return;
            }

            // Run backup if no previous backup or last backup was > 20 hours ago
            if (config.LastBackupAtUtc == null || (DateTime.UtcNow - config.LastBackupAtUtc.Value).TotalHours >= 20)
            {
                _logger.LogInformation("Initiating scheduled automatic startup backup...");
                await CreateBackupAsync(new CreateBackupRequest("ScheduledStartup", "Daily automatic startup backup"), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while evaluating startup automatic backup policy.");
        }
    }

    private async Task PruneOldBackupsAsync(int retentionCount, CancellationToken cancellationToken)
    {
        if (retentionCount <= 0) return;

        try
        {
            var records = await _backupRepository.GetAllRecordsAsync(cancellationToken);
            var completed = records.Where(r => r.Status != "Failed").OrderByDescending(r => r.CreatedAtUtc).ToList();

            if (completed.Count > retentionCount)
            {
                var toDelete = completed.Skip(retentionCount).ToList();
                foreach (var b in toDelete)
                {
                    await DeleteBackupAsync(b.Id, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during backup retention pruning.");
        }
    }
}
