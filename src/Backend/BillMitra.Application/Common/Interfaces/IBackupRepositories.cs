using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Backups;

namespace BillMitra.Application.Common.Interfaces;

public interface IBackupRepository
{
    Task<BackupRecordDto> InsertRecordAsync(BackupRecordDto record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRecordDto>> GetAllRecordsAsync(CancellationToken cancellationToken = default);
    Task<BackupRecordDto?> GetRecordByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, string status, string? checksum, CancellationToken cancellationToken = default);
    Task<bool> DeleteRecordAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BackupConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateConfigurationAsync(BackupConfigurationDto config, CancellationToken cancellationToken = default);
    Task<bool> ExecutePhysicalBackupAsync(string targetFilePath, CancellationToken cancellationToken = default);
    Task<bool> VerifyPhysicalIntegrityAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> RestorePhysicalDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default);
}
