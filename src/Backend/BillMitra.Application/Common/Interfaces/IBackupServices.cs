using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Contracts.Backups;

namespace BillMitra.Application.Common.Interfaces;

public interface IBackupService
{
    Task<BackupRecordDto> CreateBackupAsync(CreateBackupRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRecordDto>> GetBackupHistoryAsync(CancellationToken cancellationToken = default);
    Task<bool> VerifyBackupIntegrityAsync(Guid backupId, CancellationToken cancellationToken = default);
    Task<RestoreBackupResultDto> RestoreFromBackupAsync(RestoreBackupRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default);
    Task<BackupConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<BackupConfigurationDto> UpdateConfigurationAsync(UpdateBackupConfigurationRequest request, CancellationToken cancellationToken = default);
    Task ExecuteStartupAutoBackupIfNeededAsync(CancellationToken cancellationToken = default);
}
