using System;
using System.Collections.Generic;

namespace BillMitra.Contracts.Gst;

public enum GstReturnTypeDto
{
    Gstr1 = 1,
    Gstr3b = 2
}

public enum GstFilingStatusDto
{
    NotPrepared = 0,
    Prepared = 1,
    ValidationFailed = 2,
    ReadyForSubmission = 3,
    SubmittedToProvider = 4,
    Pending = 5,
    Accepted = 6,
    Rejected = 7,
    Filed = 8
}

public sealed record Gstr1SummaryDto(
    string TaxPeriod,
    string FinancialYear,
    decimal B2bTotalTaxable,
    decimal B2bTotalTax,
    int B2bCount,
    decimal B2clTotalTaxable,
    decimal B2clTotalTax,
    int B2clCount,
    decimal B2csTotalTaxable,
    decimal B2csTotalTax,
    int B2csCount,
    decimal CdnrTotalTaxable,
    decimal CdnrTotalTax,
    int CdnrCount,
    decimal CdnurTotalTaxable,
    decimal CdnurTotalTax,
    int CdnurCount,
    int HsnCount,
    int TotalInvoicesCount,
    int TotalCancelledCount,
    decimal TotalTaxableValue,
    decimal TotalCgst,
    decimal TotalSgst,
    decimal TotalIgst,
    decimal TotalCess,
    decimal TotalTaxLiability,
    decimal GrandTotal,
    IReadOnlyList<Gstr1B2bInvoiceDto> B2bInvoices,
    IReadOnlyList<Gstr1B2clInvoiceDto> B2clInvoices,
    IReadOnlyList<Gstr1B2csSectionDto> B2csSections,
    IReadOnlyList<Gstr1CdnrNoteDto> CdnrNotes,
    IReadOnlyList<Gstr1DocSummaryDto> DocumentsSummary
);

public sealed record Gstr1B2bInvoiceDto(
    string CustomerGstin,
    string CustomerName,
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal InvoiceValue,
    string PlaceOfSupply,
    bool ReverseCharge,
    string InvoiceType,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal CessAmount,
    IReadOnlyList<Gstr1ItemDetailDto> Items
);

public sealed record Gstr1B2clInvoiceDto(
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal InvoiceValue,
    string PlaceOfSupply,
    decimal TaxableValue,
    decimal IgstAmount,
    decimal CessAmount,
    IReadOnlyList<Gstr1ItemDetailDto> Items
);

public sealed record Gstr1B2csSectionDto(
    string SupplyType,
    string PlaceOfSupply,
    decimal GstRatePercent,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal CessAmount
);

public sealed record Gstr1CdnrNoteDto(
    string CustomerGstin,
    string CustomerName,
    string NoteNumber,
    DateTime NoteDate,
    string NoteType, // "C" = Credit, "D" = Debit
    string OriginalInvoiceNumber,
    DateTime? OriginalInvoiceDate,
    decimal NoteValue,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal CessAmount
);

public sealed record Gstr1DocSummaryDto(
    int DocumentType,
    string DocumentName,
    string FromSerial,
    string ToSerial,
    int TotalCount,
    int CancelledCount,
    int NetIssuedCount
);

public sealed record Gstr1ItemDetailDto(
    decimal RatePercent,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal CessAmount
);

public sealed record HsnSummaryItemDto(
    string HsnSacCode,
    string Description,
    string UnitCode,
    decimal TotalQuantity,
    decimal TotalValue,
    decimal TaxableAmount,
    decimal GstRatePercent,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal CessAmount
);

public sealed record Gstr3bSummaryDto(
    string TaxPeriod,
    string FinancialYear,
    Gstr3bTable31Dto Table31,
    IReadOnlyList<Gstr3bTable32RowDto> Table32,
    Gstr3bTable4Dto Table4,
    Gstr3bTaxLiabilityDto TotalLiability
);

public sealed record Gstr3bTable31Dto(
    Gstr3bTaxRowDto OutwardTaxableSupplies,
    Gstr3bTaxRowDto ZeroRatedSupplies,
    decimal OtherOutwardNilExempt,
    Gstr3bTaxRowDto InwardReverseChargeSupplies,
    decimal NonGstOutwardSupplies
);

public sealed record Gstr3bTable32RowDto(
    string PlaceOfSupplyStateCode,
    string StateName,
    decimal TaxableValue,
    decimal IntegratedTaxAmount
);

public sealed record Gstr3bTable4Dto(
    decimal InwardReverseChargeItc,
    decimal AllOtherItc,
    decimal NetItcAvailable
);

public sealed record Gstr3bTaxRowDto(
    decimal TaxableValue,
    decimal IntegratedTax,
    decimal CentralTax,
    decimal StateTax,
    decimal Cess
);

public sealed record Gstr3bTaxLiabilityDto(
    decimal TaxableValue,
    decimal IntegratedTax,
    decimal CentralTax,
    decimal StateTax,
    decimal Cess,
    decimal TotalTax
);

public sealed record GstValidationResultDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);

public sealed record GstReturnFilingDto(
    Guid Id,
    Guid CompanyId,
    GstReturnTypeDto ReturnType,
    string FinancialYear,
    string TaxPeriod,
    GstFilingStatusDto FilingStatus,
    string? Arn,
    DateTime? ArnDate,
    IReadOnlyList<string> ValidationErrors,
    string? ProviderName,
    string? ProviderReference,
    decimal TotalTaxableValue,
    decimal TotalTaxLiability,
    DateTime CreatedAtUtc,
    DateTime? FiledAtUtc
);

public sealed record SubmitGstReturnRequest(
    string TaxPeriod,
    bool SimulateProvider = true
);

public sealed record MarkReturnFiledRequest(
    string Arn,
    DateTime ArnDate
);

// --- Official GST Portal Offline Tool JSON Schema Models ---
public sealed class Gstr1JsonRoot
{
    public string gstin { get; set; } = string.Empty;
    public string fp { get; set; } = string.Empty;
    public decimal cur_gt { get; set; }
    public decimal gt { get; set; }
    public List<Gstr1JsonB2bGroup> b2b { get; set; } = new();
    public List<Gstr1JsonB2clGroup> b2cl { get; set; } = new();
    public List<Gstr1JsonB2csItem> b2cs { get; set; } = new();
    public List<Gstr1JsonCdnrGroup> cdnr { get; set; } = new();
    public List<Gstr1JsonCdnurGroup> cdnur { get; set; } = new();
    public Gstr1JsonHsnTable hsn { get; set; } = new();
    public Gstr1JsonDocIssueTable doc_issue { get; set; } = new();
}

public sealed class Gstr1JsonB2bGroup
{
    public string ctin { get; set; } = string.Empty;
    public List<Gstr1JsonInvoice> inv { get; set; } = new();
}

public sealed class Gstr1JsonInvoice
{
    public string inum { get; set; } = string.Empty;
    public string idt { get; set; } = string.Empty;
    public decimal val { get; set; }
    public string pos { get; set; } = string.Empty;
    public string rchrg { get; set; } = "N";
    public string inv_typ { get; set; } = "R";
    public List<Gstr1JsonInvoiceItem> itms { get; set; } = new();
}

public sealed class Gstr1JsonInvoiceItem
{
    public int num { get; set; }
    public Gstr1JsonItemDetail itm_det { get; set; } = new();
}

public sealed class Gstr1JsonItemDetail
{
    public decimal rt { get; set; }
    public decimal txval { get; set; }
    public decimal iamt { get; set; }
    public decimal camt { get; set; }
    public decimal samt { get; set; }
    public decimal csamt { get; set; }
}

public sealed class Gstr1JsonB2clGroup
{
    public string pos { get; set; } = string.Empty;
    public List<Gstr1JsonInvoice> inv { get; set; } = new();
}

public sealed class Gstr1JsonB2csItem
{
    public string sply_ty { get; set; } = "INTRA";
    public string pos { get; set; } = string.Empty;
    public string typ { get; set; } = "OE";
    public decimal rt { get; set; }
    public decimal txval { get; set; }
    public decimal iamt { get; set; }
    public decimal camt { get; set; }
    public decimal samt { get; set; }
    public decimal csamt { get; set; }
}

public sealed class Gstr1JsonCdnrGroup
{
    public string ctin { get; set; } = string.Empty;
    public List<Gstr1JsonCdnrNote> nt { get; set; } = new();
}

public sealed class Gstr1JsonCdnrNote
{
    public string ntty { get; set; } = "C";
    public string nt_num { get; set; } = string.Empty;
    public string nt_dt { get; set; } = string.Empty;
    public string inum { get; set; } = string.Empty;
    public string idt { get; set; } = string.Empty;
    public decimal val { get; set; }
    public string pos { get; set; } = string.Empty;
    public string rchrg { get; set; } = "N";
    public List<Gstr1JsonInvoiceItem> itms { get; set; } = new();
}

public sealed class Gstr1JsonCdnurGroup
{
    public string typ { get; set; } = "B2CL";
    public string ntty { get; set; } = "C";
    public string nt_num { get; set; } = string.Empty;
    public string nt_dt { get; set; } = string.Empty;
    public string inum { get; set; } = string.Empty;
    public string idt { get; set; } = string.Empty;
    public decimal val { get; set; }
    public string pos { get; set; } = string.Empty;
    public List<Gstr1JsonInvoiceItem> itms { get; set; } = new();
}

public sealed class Gstr1JsonHsnTable
{
    public List<Gstr1JsonHsnItem> data { get; set; } = new();
}

public sealed class Gstr1JsonHsnItem
{
    public int num { get; set; }
    public string hsn_sc { get; set; } = string.Empty;
    public string desc { get; set; } = string.Empty;
    public string uqc { get; set; } = "PCS";
    public decimal qty { get; set; }
    public decimal val { get; set; }
    public decimal txval { get; set; }
    public decimal iamt { get; set; }
    public decimal camt { get; set; }
    public decimal samt { get; set; }
    public decimal csamt { get; set; }
}

public sealed class Gstr1JsonDocIssueTable
{
    public List<Gstr1JsonDocGroup> doc_det { get; set; } = new();
}

public sealed class Gstr1JsonDocGroup
{
    public int doc_num { get; set; }
    public List<Gstr1JsonDocRow> docs { get; set; } = new();
}

public sealed class Gstr1JsonDocRow
{
    public int num { get; set; }
    public string from { get; set; } = string.Empty;
    public string to { get; set; } = string.Empty;
    public int totnum { get; set; }
    public int canc { get; set; }
    public int net_issue { get; set; }
}
