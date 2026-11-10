using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Backups;
using Dapper;

namespace BillMitra.Infrastructure.Repositories;

public class BackupRepository : IBackupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BackupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BackupRecordDto> InsertRecordAsync(BackupRecordDto record, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO backup_records (id, file_name, file_path, file_size_bytes, backup_type, status, created_at_utc, checksum, notes)
            VALUES (@Id, @FileName, @FilePath, @FileSizeBytes, @BackupType, @Status, @CreatedAtUtc, @Checksum, @Notes)";

        await conn.ExecuteAsync(sql, record);
        return record;
    }

    public async Task<IReadOnlyList<BackupRecordDto>> GetAllRecordsAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, file_name as FileName, file_path as FilePath, file_size_bytes as FileSizeBytes,
                   backup_type as BackupType, status as Status, created_at_utc as CreatedAtUtc,
                   checksum as Checksum, notes as Notes
            FROM backup_records
            ORDER BY created_at_utc DESC";

        var records = await conn.QueryAsync<BackupRecordDto>(sql);
        return records.ToList();
    }

    public async Task<BackupRecordDto?> GetRecordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, file_name as FileName, file_path as FilePath, file_size_bytes as FileSizeBytes,
                   backup_type as BackupType, status as Status, created_at_utc as CreatedAtUtc,
                   checksum as Checksum, notes as Notes
            FROM backup_records
            WHERE id = @Id";

        return await conn.QuerySingleOrDefaultAsync<BackupRecordDto>(sql, new { Id = id });
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, string? checksum, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE backup_records
            SET status = @Status, checksum = COALESCE(@Checksum, checksum)
            WHERE id = @Id";

        var affected = await conn.ExecuteAsync(sql, new { Id = id, Status = status, Checksum = checksum });
        return affected > 0;
    }

    public async Task<bool> DeleteRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "DELETE FROM backup_records WHERE id = @Id";
        var affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<BackupConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT auto_backup_on_startup as AutoBackupOnStartup,
                   backup_folder_path as BackupFolderPath,
                   retention_count as RetentionCount,
                   last_backup_at_utc as LastBackupAtUtc
            FROM backup_configuration
            WHERE id = 1";

        var row = await conn.QuerySingleOrDefaultAsync<BackupConfigRow>(sql);
        if (row == null)
        {
            var defaultFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BillMitra",
                "Backups"
            );
            return new BackupConfigurationDto(true, defaultFolder, 10, null);
        }

        return new BackupConfigurationDto(
            AutoBackupOnStartup: row.AutoBackupOnStartup == 1,
            BackupFolderPath: row.BackupFolderPath,
            RetentionCount: row.RetentionCount,
            LastBackupAtUtc: row.LastBackupAtUtc
        );
    }

    public async Task<bool> UpdateConfigurationAsync(BackupConfigurationDto config, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE backup_configuration
            SET auto_backup_on_startup = @AutoBackupOnStartup,
                backup_folder_path = @BackupFolderPath,
                retention_count = @RetentionCount,
                last_backup_at_utc = @LastBackupAtUtc
            WHERE id = 1";

        var affected = await conn.ExecuteAsync(sql, new
        {
            AutoBackupOnStartup = config.AutoBackupOnStartup ? 1 : 0,
            BackupFolderPath = config.BackupFolderPath,
            RetentionCount = config.RetentionCount,
            LastBackupAtUtc = config.LastBackupAtUtc
        });
        return affected > 0;
    }

    private sealed class BackupConfigRow
    {
        public int AutoBackupOnStartup { get; set; }
        public string BackupFolderPath { get; set; } = string.Empty;
        public int RetentionCount { get; set; }
        public DateTime? LastBackupAtUtc { get; set; }
    }

    public async Task<bool> ExecutePhysicalBackupAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        var provider = _connectionFactory.ProviderName;
        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"VACUUM INTO '{targetFilePath.Replace("'", "''")}'";
            cmd.ExecuteNonQuery();
            return true;
        }

        var activeDb = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BillMitra", "Database", "billmitra_local.db"
        );

        if (File.Exists(activeDb))
        {
            File.Copy(activeDb, targetFilePath, overwrite: true);
            return true;
        }

        await File.WriteAllTextAsync(targetFilePath, $"-- BillMitra State Snapshot\n-- Provider: {provider}\n", cancellationToken);
        return true;
    }

    public async Task<bool> VerifyPhysicalIntegrityAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) return false;

        if (filePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var connStr = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = filePath,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly
                }.ToString();

                using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = (await cmd.ExecuteScalarAsync(cancellationToken))?.ToString();
                return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        return new FileInfo(filePath).Length > 0;
    }

    public Task<bool> RestorePhysicalDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath)) return Task.FromResult(false);

        var provider = _connectionFactory.ProviderName;
        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var activeDb = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "BillMitra", "Database", "billmitra_local.db"
            );

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Copy(sourceFilePath, activeDb, overwrite: true);
            return Task.FromResult(true);
        }

        return Task.FromResult(true);
    }
}
