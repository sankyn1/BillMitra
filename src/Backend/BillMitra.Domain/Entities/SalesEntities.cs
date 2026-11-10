using System;
using System.Collections.Generic;
using BillMitra.Domain.Enums;

namespace BillMitra.Domain.Entities;

public class SalesDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DocumentType DocumentType { get; set; } = DocumentType.TaxInvoice;
    public DocumentStatus DocumentStatus { get; set; } = DocumentStatus.Draft;
    public string? DocumentNumber { get; set; }
    public string DraftNumber { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? PartyId { get; set; }

    // Snapshot fields (immutable once finalized)
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerGstin { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string PlaceOfSupplyStateCode { get; set; } = string.Empty;
    public string SupplierStateCode { get; set; } = string.Empty;
    public bool IsReverseCharge { get; set; }
    public SupplyType SupplyType { get; set; } = SupplyType.IntraState;
    public string CurrencyCode { get; set; } = "INR";

    // Financial totals
    public decimal SubTotalTaxable { get; set; }
    public decimal CgstTotal { get; set; }
    public decimal SgstTotal { get; set; }
    public decimal IgstTotal { get; set; }
    public decimal CessTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal RoundOffAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string TotalAmountInWords { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public string? CreatedBy { get; set; }

    public List<SalesDocumentItem> Items { get; set; } = new();

    public bool CanFinalize() => DocumentStatus == DocumentStatus.Draft;
    public bool CanCancel() => DocumentStatus == DocumentStatus.Finalized;
}

public class SalesDocumentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesDocumentId { get; set; }
    public Guid? ProductId { get; set; }
    public int LineNumber { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string HsnSacCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "PCS";
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRatePercent { get; set; }
    public decimal CgstRatePercent { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstRatePercent { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstRatePercent { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessRatePercent { get; set; }
    public decimal CessAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DocumentSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FinancialYearId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string Prefix { get; set; } = "INV";
    public string FinancialYearToken { get; set; } = "26-27";
    public long NextNumber { get; set; } = 1;
    public int PaddingLength { get; set; } = 4;
    public string? Suffix { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Generates the next formatted document number conforming to CBIC Rule 46 (max 16 chars).
    /// Example: INV/26-27/0001 (14 chars)
    /// </summary>
    public string FormatNumber(long number)
    {
        var seqStr = number.ToString().PadLeft(PaddingLength, '0');
        var formatted = string.IsNullOrEmpty(Suffix)
            ? $"{Prefix}/{FinancialYearToken}/{seqStr}"
            : $"{Prefix}/{FinancialYearToken}/{seqStr}/{Suffix}";

        if (formatted.Length > 16 && DocumentType == DocumentType.TaxInvoice)
        {
            throw new InvalidOperationException(
                $"Generated invoice number '{formatted}' exceeds the maximum statutory limit of 16 characters permitted under Rule 46 of CGST Rules.");
        }

        return formatted;
    }
}

public static class IndianCurrencyHelper
{
    private static readonly string[] Units = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
    private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    public static string ConvertToWords(decimal amount)
    {
        if (amount == 0) return "Rupees Zero Only";

        var positiveAmount = Math.Abs(amount);
        var wholePart = (long)Math.Truncate(positiveAmount);
        var paisePart = (int)Math.Round((positiveAmount - wholePart) * 100);

        var words = "Rupees " + NumberToWords(wholePart).Trim();

        if (paisePart > 0)
        {
            words += " and " + NumberToWords(paisePart).Trim() + " Paise";
        }

        return words + " Only";
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "";

        if (number >= 10000000) // Crores
        {
            return NumberToWords(number / 10000000) + " Crore " + NumberToWords(number % 10000000);
        }
        if (number >= 100000) // Lakhs
        {
            return NumberToWords(number / 100000) + " Lakh " + NumberToWords(number % 100000);
        }
        if (number >= 1000) // Thousands
        {
            return NumberToWords(number / 1000) + " Thousand " + NumberToWords(number % 1000);
        }
        if (number >= 100) // Hundreds
        {
            return NumberToWords(number / 100) + " Hundred " + NumberToWords(number % 100);
        }

        if (number >= 20)
        {
            return Tens[number / 10] + (number % 10 > 0 ? " " + Units[number % 10] : "");
        }

        return Units[number];
    }
}
