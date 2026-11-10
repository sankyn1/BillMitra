using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using BillMitra.Contracts.Purchases;
using BillMitra.Domain.Entities;
using BillMitra.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillMitra.UnitTests;

public class PurchaseExpenseUnitTests
{
    [Fact]
    public async Task CreateDraftAsync_IntraStatePurchase_CalculatesCgstAndSgstCorrectly()
    {
        // Arrange
        var companyRepoMock = new Mock<ICompanyRepository>();
        var purchaseRepoMock = new Mock<IPurchaseRepository>();

        var company = new CompanyProfile
        {
            Id = Guid.NewGuid(),
            LegalName = "Acme Corp",
            Gstin = "24AABCA1234F1Z1",
            StateCode = "24",
            StateName = "Gujarat",
            City = "Ahmedabad",
            Pincode = "380001",
            Email = "info@acme.com",
            Phone = "9876543210"
        };

        companyRepoMock
            .Setup(c => c.GetPrimaryCompanyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        purchaseRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<PurchaseDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var service = new PurchaseService(purchaseRepoMock.Object, companyRepoMock.Object, NullLogger<PurchaseService>.Instance);

        var request = new CreateOrUpdatePurchaseRequest(
            DocumentType: (int)PurchaseDocumentType.PurchaseInvoice,
            SupplierId: Guid.NewGuid(),
            SupplierName: "Local Steel Suppliers",
            SupplierGstin: "24XYZPA1234L1Z2",
            SupplierAddress: "Plot 10, GIDC Vatva, Ahmedabad",
            SupplierStateCode: "24", // Intra-state (24 == 24)
            PlaceOfSupplyStateCode: "24",
            SupplierInvoiceNumber: "INV-2026-001",
            SupplierInvoiceDate: DateTime.UtcNow,
            DocumentDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddDays(30),
            IsReverseCharge: false,
            DefaultItcEligibility: (int)ItcEligibility.EligibleOther,
            Notes: "Intra-state raw material purchase",
            Items: new List<PurchaseDocumentItemInput>
            {
                new PurchaseDocumentItemInput(
                    ProductId: Guid.NewGuid(),
                    ItemName: "Steel Rods 10mm",
                    HsnCode: "7214",
                    Quantity: 10,
                    Unit: "KGS",
                    UnitPrice: 1000m,
                    DiscountPercent: 10m, // Taxable: 9000
                    GstRate: 18m,          // 9% CGST (810) + 9% SGST (810) = 1620
                    ItcEligibility: (int)ItcEligibility.EligibleOther
                )
            }
        );

        // Act
        var result = await service.CreateDraftAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SubTotalTaxable.Should().Be(9000m);
        result.CgstTotal.Should().Be(810m);
        result.SgstTotal.Should().Be(810m);
        result.IgstTotal.Should().Be(0m);
        result.TotalTax.Should().Be(1620m);
        result.GrandTotal.Should().Be(10620m);
    }

    [Fact]
    public async Task CreateDraftAsync_InterStatePurchase_CalculatesIgstCorrectly()
    {
        // Arrange
        var companyRepoMock = new Mock<ICompanyRepository>();
        var purchaseRepoMock = new Mock<IPurchaseRepository>();

        var company = new CompanyProfile
        {
            Id = Guid.NewGuid(),
            LegalName = "Acme Corp",
            Gstin = "24AABCA1234F1Z1",
            StateCode = "24",
            StateName = "Gujarat",
            City = "Ahmedabad",
            Pincode = "380001",
            Email = "info@acme.com",
            Phone = "9876543210"
        };

        companyRepoMock
            .Setup(c => c.GetPrimaryCompanyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        purchaseRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<PurchaseDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var service = new PurchaseService(purchaseRepoMock.Object, companyRepoMock.Object, NullLogger<PurchaseService>.Instance);

        var request = new CreateOrUpdatePurchaseRequest(
            DocumentType: (int)PurchaseDocumentType.PurchaseInvoice,
            SupplierId: Guid.NewGuid(),
            SupplierName: "Maharashtra Tech Spares",
            SupplierGstin: "27XYZPA1234L1Z2",
            SupplierAddress: "Andheri East, Mumbai",
            SupplierStateCode: "27", // Inter-state (27 != 24)
            PlaceOfSupplyStateCode: "24",
            SupplierInvoiceNumber: "MUM-2026-99",
            SupplierInvoiceDate: DateTime.UtcNow,
            DocumentDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddDays(15),
            IsReverseCharge: false,
            DefaultItcEligibility: (int)ItcEligibility.EligibleOther,
            Notes: "Inter-state spares",
            Items: new List<PurchaseDocumentItemInput>
            {
                new PurchaseDocumentItemInput(
                    ProductId: Guid.NewGuid(),
                    ItemName: "Server Power Unit",
                    HsnCode: "8504",
                    Quantity: 2,
                    Unit: "NOS",
                    UnitPrice: 5000m,
                    DiscountPercent: 0m,  // Taxable: 10000
                    GstRate: 18m,          // IGST: 1800
                    ItcEligibility: (int)ItcEligibility.EligibleCapitalGoods
                )
            }
        );

        // Act
        var result = await service.CreateDraftAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SubTotalTaxable.Should().Be(10000m);
        result.CgstTotal.Should().Be(0m);
        result.SgstTotal.Should().Be(0m);
        result.IgstTotal.Should().Be(1800m);
        result.TotalTax.Should().Be(1800m);
        result.GrandTotal.Should().Be(11800m);
    }

    [Fact]
    public async Task ExpenseService_CreateExpense_ComputesGstAndVoucherSeq()
    {
        // Arrange
        var expenseRepoMock = new Mock<IExpenseRepository>();
        var companyRepoMock = new Mock<ICompanyRepository>();

        var catId = Guid.NewGuid();
        var category = new ExpenseCategory
        {
            Id = catId,
            Name = "Office Supplies",
            Description = "Stationery and pantry",
            IsActive = true
        };

        var company = new CompanyProfile
        {
            Id = Guid.NewGuid(),
            LegalName = "Acme Corp"
        };

        companyRepoMock
            .Setup(c => c.GetPrimaryCompanyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        expenseRepoMock
            .Setup(r => r.GetCategoryByIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        expenseRepoMock
            .Setup(r => r.GetNextVoucherNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("EXP/26-27/0001");

        expenseRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<ExpenseEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var service = new ExpenseService(expenseRepoMock.Object, companyRepoMock.Object, NullLogger<ExpenseService>.Instance);

        var request = new CreateExpenseRequest(
            ExpenseCategoryId: catId,
            ExpenseDate: DateTime.UtcNow,
            Amount: 1000m,
            GstRate: 18m,
            IsItcClaimable: true,
            PaymentMode: (int)PaymentMode.UPI,
            ReferenceNumber: "UPI-TXN-8877",
            PayeeName: "Stationery Mart",
            Notes: "Printer paper and ink cartridges"
        );

        // Act
        var result = await service.CreateExpenseAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.VoucherNumber.Should().Be("EXP/26-27/0001");
        result.Amount.Should().Be(1000m);
        result.GstRate.Should().Be(18m);
        result.GstAmount.Should().Be(180m);
        result.TotalAmount.Should().Be(1180m);
        result.IsItcClaimable.Should().BeTrue();
    }
}
