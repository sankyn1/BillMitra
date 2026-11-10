using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using BillMitra.Contracts.Backups;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillMitra.UnitTests;

public class BackupUnitTests
{
    private readonly Mock<IBackupRepository> _mockRepo;
    private readonly BackupService _service;

    public BackupUnitTests()
    {
        _mockRepo = new Mock<IBackupRepository>();
        _service = new BackupService(_mockRepo.Object, NullLogger<BackupService>.Instance);
    }

    [Fact]
    public async Task GetConfiguration_ReturnsConfigFromRepository()
    {
        var config = new BackupConfigurationDto(true, "C:\\TestBackups", 10, DateTime.UtcNow);
        _mockRepo.Setup(r => r.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await _service.GetConfigurationAsync();

        result.Should().NotBeNull();
        result.BackupFolderPath.Should().Be("C:\\TestBackups");
        result.RetentionCount.Should().Be(10);
        result.AutoBackupOnStartup.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateConfiguration_ModifiesAndReturnsUpdatedConfig()
    {
        var existing = new BackupConfigurationDto(true, "C:\\TestBackups", 10, null);
        _mockRepo.Setup(r => r.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateConfigurationAsync(It.IsAny<BackupConfigurationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateBackupConfigurationRequest(false, "D:\\NewBackups", 15);
        var result = await _service.UpdateConfigurationAsync(request);

        result.AutoBackupOnStartup.Should().BeFalse();
        result.BackupFolderPath.Should().Be("D:\\NewBackups");
        result.RetentionCount.Should().Be(15);
        _mockRepo.Verify(r => r.UpdateConfigurationAsync(It.Is<BackupConfigurationDto>(c => c.RetentionCount == 15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBackup_ExecutesPhysicalBackupAndInsertsRecord()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "BillMitraBackupTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempFolder);

        try
        {
            var config = new BackupConfigurationDto(true, tempFolder, 10, null);
            _mockRepo.Setup(r => r.GetConfigurationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            _mockRepo.Setup(r => r.ExecutePhysicalBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((path, ct) => File.WriteAllText(path, "TEST_DB_DATA"))
                .ReturnsAsync(true);
            _mockRepo.Setup(r => r.InsertRecordAsync(It.IsAny<BackupRecordDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BackupRecordDto r, CancellationToken ct) => r);
            _mockRepo.Setup(r => r.GetAllRecordsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<BackupRecordDto>());

            var request = new CreateBackupRequest("Manual", "Test backup execution");
            var result = await _service.CreateBackupAsync(request);

            result.Should().NotBeNull();
            result.Status.Should().Be("Completed");
            result.BackupType.Should().Be("Manual");
            result.Checksum.Should().NotBeNullOrEmpty();
            File.Exists(result.FilePath).Should().BeTrue();

            _mockRepo.Verify(r => r.ExecutePhysicalBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRepo.Verify(r => r.InsertRecordAsync(It.IsAny<BackupRecordDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }
}
