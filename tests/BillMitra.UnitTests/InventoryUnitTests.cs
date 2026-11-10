using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using BillMitra.Contracts.Inventory;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillMitra.UnitTests;

public class InventoryUnitTests
{
    [Fact]
    public void StockItemBatch_ExpiryDetection_FlagsCorrectly()
    {
        var expiredBatch = new StockItemBatch
        {
            BatchNumber = "BATCH-EXP",
            ExpiryDate = DateTime.UtcNow.AddDays(-5)
        };

        var expiringSoonBatch = new StockItemBatch
        {
            BatchNumber = "BATCH-SOON",
            ExpiryDate = DateTime.UtcNow.AddDays(15)
        };

        var safeBatch = new StockItemBatch
        {
            BatchNumber = "BATCH-SAFE",
            ExpiryDate = DateTime.UtcNow.AddDays(90)
        };

        expiredBatch.IsExpired().Should().BeTrue();
        expiredBatch.IsExpiringSoon(30).Should().BeFalse();

        expiringSoonBatch.IsExpired().Should().BeFalse();
        expiringSoonBatch.IsExpiringSoon(30).Should().BeTrue();

        safeBatch.IsExpired().Should().BeFalse();
        safeBatch.IsExpiringSoon(30).Should().BeFalse();
    }

    [Fact]
    public async Task InventoryService_RecordAdjustment_CalculatesDeltaAndInsertsMovement()
    {
        var inventoryRepoMock = new Mock<IInventoryRepository>();
        var warehouseRepoMock = new Mock<IWarehouseRepository>();
        var productRepoMock = new Mock<IProductRepository>();
        var companyRepoMock = new Mock<ICompanyRepository>();

        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Aluminium Sheets 2mm",
            ItemCode = "ALU-002",
            PurchasePrice = 250m,
            SalePrice = 320m
        };

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Name = "Raw Material Store",
            Code = "RMS"
        };

        productRepoMock
            .Setup(p => p.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        warehouseRepoMock
            .Setup(w => w.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        inventoryRepoMock
            .Setup(r => r.GetProductStockBalanceAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m); // System says 100 units

        inventoryRepoMock
            .Setup(r => r.InsertAdjustmentAsync(It.IsAny<StockAdjustmentRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        StockMovementRecord? capturedMovement = null;
        inventoryRepoMock
            .Setup(r => r.InsertMovementAsync(It.IsAny<StockMovementRecord>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovementRecord, CancellationToken>((m, _) => capturedMovement = m)
            .ReturnsAsync(Guid.NewGuid());

        var service = new InventoryService(
            inventoryRepoMock.Object,
            warehouseRepoMock.Object,
            productRepoMock.Object,
            companyRepoMock.Object,
            NullLogger<InventoryService>.Instance
        );

        // Act: Physical count reveals only 95 units (5 damaged/missing)
        var request = new StockAdjustmentRequest(
            ProductId: productId,
            WarehouseId: warehouseId,
            BatchId: null,
            Reason: (int)StockAdjustmentReason.DamagedOrBroken,
            PhysicalQuantity: 95m,
            Remarks: "Damaged during handling"
        );

        var result = await service.RecordAdjustmentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SystemQuantity.Should().Be(100m);
        result.PhysicalQuantity.Should().Be(95m);
        result.AdjustmentQuantity.Should().Be(-5m);
        result.UnitCost.Should().Be(250m);
        result.ImpactValue.Should().Be(-1250m);

        capturedMovement.Should().NotBeNull();
        capturedMovement!.MovementType.Should().Be(StockMovementType.AdjustmentReduction);
        capturedMovement.Quantity.Should().Be(-5m);
        capturedMovement.BalanceQuantityAfter.Should().Be(95m);
    }

    [Fact]
    public async Task InventoryService_RecordTransfer_GeneratesDualMovements()
    {
        var inventoryRepoMock = new Mock<IInventoryRepository>();
        var warehouseRepoMock = new Mock<IWarehouseRepository>();
        var productRepoMock = new Mock<IProductRepository>();
        var companyRepoMock = new Mock<ICompanyRepository>();

        var productId = Guid.NewGuid();
        var sourceWhId = Guid.NewGuid();
        var destWhId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Copper Wire Spool",
            ItemCode = "COP-01",
            PurchasePrice = 1200m
        };

        productRepoMock
            .Setup(p => p.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        warehouseRepoMock
            .Setup(w => w.GetByIdAsync(sourceWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Warehouse { Id = sourceWhId, Name = "Main Warehouse", Code = "MAIN" });

        warehouseRepoMock
            .Setup(w => w.GetByIdAsync(destWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Warehouse { Id = destWhId, Name = "Branch Showroom", Code = "BRANCH" });

        inventoryRepoMock
            .Setup(r => r.GetNextTransferNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("TRF/26-27/0001");

        inventoryRepoMock
            .Setup(r => r.GetProductStockBalanceAsync(productId, sourceWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m);

        inventoryRepoMock
            .Setup(r => r.GetProductStockBalanceAsync(productId, destWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10m);

        var movements = new List<StockMovementRecord>();
        inventoryRepoMock
            .Setup(r => r.InsertMovementAsync(It.IsAny<StockMovementRecord>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovementRecord, CancellationToken>((m, _) => movements.Add(m))
            .ReturnsAsync(Guid.NewGuid());

        var service = new InventoryService(
            inventoryRepoMock.Object,
            warehouseRepoMock.Object,
            productRepoMock.Object,
            companyRepoMock.Object,
            NullLogger<InventoryService>.Instance
        );

        var transferReq = new InterWarehouseTransferRequest(
            SourceWarehouseId: sourceWhId,
            DestinationWarehouseId: destWhId,
            ProductId: productId,
            Quantity: 20m,
            Remarks: "Replenishing branch stock"
        );

        // Act
        var result = await service.RecordTransferAsync(transferReq);

        // Assert
        result.Should().NotBeNull();
        result.TransferNumber.Should().Be("TRF/26-27/0001");
        result.Quantity.Should().Be(20m);

        movements.Should().HaveCount(2);
        var outMovement = movements[0];
        outMovement.MovementType.Should().Be(StockMovementType.TransferOut);
        outMovement.Quantity.Should().Be(-20m);
        outMovement.BalanceQuantityAfter.Should().Be(30m); // 50 - 20

        var inMovement = movements[1];
        inMovement.MovementType.Should().Be(StockMovementType.TransferIn);
        inMovement.Quantity.Should().Be(20m);
        inMovement.BalanceQuantityAfter.Should().Be(30m); // 10 + 20
    }
}

