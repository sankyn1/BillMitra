using System;

namespace BillMitra.Contracts.Backups;

public record BackupRecordDto(
    Guid Id,
    string FileName,
    string FilePath,
    long FileSizeBytes,
    string BackupType, // Manual, ScheduledStartup, PreRestoreSnapshot
    string Status, // Completed, Verified, Failed
    DateTime CreatedAtUtc,
    string? Checksum,
    string? Notes
);

public record CreateBackupRequest(
    string? BackupType = "Manual",
    string? Notes = null
);

public record RestoreBackupRequest(
    Guid BackupId,
    bool ConfirmSafetySnapshot = true
);

public record RestoreBackupResultDto(
    bool Success,
    string Message,
    string? SafetySnapshotFileName,
    DateTime RestoredAtUtc
);

public record BackupConfigurationDto(
    bool AutoBackupOnStartup,
    string BackupFolderPath,
    int RetentionCount,
    DateTime? LastBackupAtUtc
);

public record UpdateBackupConfigurationRequest(
    bool AutoBackupOnStartup,
    string? BackupFolderPath,
    int RetentionCount
);
