using System;
using System.Linq;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.Company;
using BillMitra.Contracts.EWayInvoice;
using BillMitra.Contracts.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillMitra.Infrastructure.Services;

public class QuestPdfGeneratorService : IPdfGeneratorService
{
    static QuestPdfGeneratorService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerateDocumentPdf(SalesDocumentDto document, CompanyProfileDto company, string format, bool withLetterhead = true)
    {
        var fmt = (format ?? "a4").Trim().ToLowerInvariant();

        var pdfDoc = Document.Create(container =>
        {
            if (fmt == "thermal")
            {
                ComposeThermalDocument(container, document, company);
            }
            else
            {
                ComposeStandardDocument(container, document, company, isA5: fmt == "a5", withLetterhead: withLetterhead);
            }
        });

        return pdfDoc.GeneratePdf();
    }

    private static void ComposeStandardDocument(IDocumentContainer container, SalesDocumentDto doc, CompanyProfileDto company, bool isA5, bool withLetterhead)
    {
        container.Page(page =>
        {
            page.Size(isA5 ? PageSizes.A5.Landscape() : PageSizes.A4);
            if (!withLetterhead)
            {
                var topMarginMm = company.PreprintedHeaderMarginMm > 0 ? company.PreprintedHeaderMarginMm : 50;
                page.MarginTop(topMarginMm, Unit.Millimetre);
                page.MarginLeft(isA5 ? 18 : 24);
                page.MarginRight(isA5 ? 18 : 24);
                page.MarginBottom(isA5 ? 18 : 24);
            }
            else
            {
                page.Margin(isA5 ? 18 : 24);
            }

            page.DefaultTextStyle(x => x.FontSize(isA5 ? 8 : 9).FontFamily("Lato"));

            page.Header().Element(c => ComposeHeader(c, doc, company, isA5, withLetterhead));
            page.Content().Element(c => ComposeBody(c, doc, isA5));
            page.Footer().Element(c => ComposeFooter(c, doc, company, isA5));
        });
    }

    private static void ComposeHeader(IContainer container, SalesDocumentDto doc, CompanyProfileDto company, bool isA5, bool withLetterhead)
    {
        var docTitle = doc.DocumentType switch
        {
            DocumentType.TaxInvoice => "TAX INVOICE",
            DocumentType.BillOfSupply => "BILL OF SUPPLY",
            DocumentType.Quotation => "QUOTATION / ESTIMATE",
            DocumentType.ProformaInvoice => "PROFORMA INVOICE",
            DocumentType.CreditNote => "CREDIT NOTE",
            DocumentType.DebitNote => "DEBIT NOTE",
            DocumentType.DeliveryChallan => "DELIVERY CHALLAN",
            _ => "SALES INVOICE"
        };

        container.Column(col =>
        {
            // Custom Letterhead image header if configured & exists on disk
            if (withLetterhead && !string.IsNullOrWhiteSpace(company.LetterheadImagePath) && System.IO.File.Exists(company.LetterheadImagePath))
            {
                col.Item().PaddingBottom(6).Image(company.LetterheadImagePath).FitWidth();
            }

            // Title & Status
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text(docTitle).Bold().FontSize(isA5 ? 13 : 16).FontColor("#1E3A8A");
                    titleCol.Item().Text(doc.DocumentStatus == DocumentStatus.Finalized ? "Original for Recipient" : $"[ {doc.DocumentStatus.ToString().ToUpperInvariant()} ]").FontSize(isA5 ? 7 : 8).FontColor("#475569");
                });

                row.ConstantItem(220).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text(text =>
                    {
                        text.Span("Document No: ").Bold();
                        text.Span(doc.DocumentNumber ?? doc.DraftNumber).Bold().FontColor("#1E3A8A");
                    });
                    metaCol.Item().Text($"Date: {doc.DocumentDate:dd-MMM-yyyy}");
                    if (doc.DueDate.HasValue)
                    {
                        metaCol.Item().Text($"Due Date: {doc.DueDate.Value:dd-MMM-yyyy}");
                    }
                    if (!string.IsNullOrWhiteSpace(doc.SourceDocumentNumber))
                    {
                        metaCol.Item().Text(text =>
                        {
                            text.Span("Ref / Source: ").Bold();
                            text.Span(doc.SourceDocumentNumber).FontColor("#1E3A8A");
                        });
                    }
                    metaCol.Item().Text($"Place of Supply: State Code {doc.PlaceOfSupplyStateCode}");
                    metaCol.Item().Text($"Reverse Charge: {(doc.IsReverseCharge ? "YES" : "NO")}");
                });
            });

            col.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#CBD5E1");

            // Company vs Party details
            col.Item().Row(row =>
            {
                if (withLetterhead)
                {
                    // Supplier Box
                    row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(suppCol =>
                    {
                        suppCol.Item().Text("SUPPLIER (ISSUED BY):").Bold().FontSize(isA5 ? 7 : 8).FontColor("#1E3A8A");
                        suppCol.Item().Text(company.LegalName).Bold().FontSize(isA5 ? 9 : 10);
                        if (!string.IsNullOrWhiteSpace(company.TradeName) && company.TradeName != company.LegalName)
                        {
                            suppCol.Item().Text($"({company.TradeName})").Italic();
                        }
                        suppCol.Item().Text($"{company.AddressLine1}, {company.City} - {company.Pincode}");
                        suppCol.Item().Text(text =>
                        {
                            text.Span("GSTIN: ").Bold();
                            text.Span(company.Gstin ?? "Unregistered").Bold();
                        });
                        suppCol.Item().Text($"State: {company.StateName} ({company.StateCode}) | PAN: {company.Pan}");
                        if (!string.IsNullOrWhiteSpace(company.Phone))
                        {
                            suppCol.Item().Text($"Phone: {company.Phone} | Email: {company.Email}");
                        }
                    });

                    row.ConstantItem(10);
                }

                // Recipient Box
                row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(custCol =>
                {
                    custCol.Item().Text("BILLED TO (RECIPIENT):").Bold().FontSize(isA5 ? 7 : 8).FontColor("#1E3A8A");
                    custCol.Item().Text(doc.CustomerName).Bold().FontSize(isA5 ? 9 : 10);
                    if (!string.IsNullOrWhiteSpace(doc.BillingAddress))
                    {
                        custCol.Item().Text(doc.BillingAddress);
                    }
                    custCol.Item().Text(text =>
                    {
                        text.Span("GSTIN: ").Bold();
                        text.Span(string.IsNullOrWhiteSpace(doc.CustomerGstin) ? "Unregistered / Consumer" : doc.CustomerGstin).Bold();
                    });
                    custCol.Item().Text($"Place of Supply: State Code {doc.PlaceOfSupplyStateCode}");
                    if (!string.IsNullOrWhiteSpace(doc.ShippingAddress) && doc.ShippingAddress != doc.BillingAddress)
                    {
                        custCol.Item().Text($"Shipped To: {doc.ShippingAddress}").Italic().FontSize(isA5 ? 7 : 8);
                    }
                });
            });

            col.Item().Height(8);
        });
    }

    private static void ComposeBody(IContainer container, SalesDocumentDto doc, bool isA5)
    {
        var isIntra = doc.SupplyType == GstSupplyType.IntraState;

        container.Column(col =>
        {
            // Line items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(22); // #
                    columns.RelativeColumn(3);  // Description
                    columns.ConstantColumn(50); // HSN
                    columns.ConstantColumn(40); // Qty
                    columns.ConstantColumn(30); // Unit
                    columns.ConstantColumn(55); // Rate
                    columns.ConstantColumn(40); // Disc%
                    columns.ConstantColumn(60); // Taxable
                    if (isIntra)
                    {
                        columns.ConstantColumn(50); // CGST
                        columns.ConstantColumn(50); // SGST
                    }
                    else
                    {
                        columns.ConstantColumn(55); // IGST
                    }
                    columns.ConstantColumn(65); // Total
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("#");
                    header.Cell().Element(HeaderCellStyle).Text("Item & Description");
                    header.Cell().Element(HeaderCellStyle).Text("HSN/SAC");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCellStyle).Text("Unit");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Rate (₹)");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Disc%");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Taxable");
                    if (isIntra)
                    {
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("CGST");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("SGST");
                    }
                    else
                    {
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("IGST");
                    }
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount (₹)");

                    static IContainer HeaderCellStyle(IContainer c) =>
                        c.Border(1).BorderColor("#CBD5E1").Background("#F1F5F9").Padding(4);
                });

                int idx = 1;
                foreach (var item in doc.Items)
                {
                    table.Cell().Element(BodyCellStyle).Text(idx.ToString());
                    table.Cell().Element(BodyCellStyle).Text(item.ItemName).Bold();
                    table.Cell().Element(BodyCellStyle).Text(item.HsnSacCode);
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.Quantity:0.##}");
                    table.Cell().Element(BodyCellStyle).Text(item.UnitCode);
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.UnitPrice:N2}");
                    table.Cell().Element(BodyCellStyle).AlignRight().Text(item.DiscountPercent > 0 ? $"{item.DiscountPercent}%" : "-");
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.TaxableAmount:N2}");
                    if (isIntra)
                    {
                        table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.CgstAmount:N2}\n({item.CgstRatePercent}%)");
                        table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.SgstAmount:N2}\n({item.SgstRatePercent}%)");
                    }
                    else
                    {
                        table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.IgstAmount:N2}\n({item.IgstRatePercent}%)");
                    }
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{item.TotalAmount:N2}").Bold();

                    idx++;
                }

                static IContainer BodyCellStyle(IContainer c) =>
                    c.Border(1).BorderColor("#E2E8F0").Padding(4);
            });

            col.Item().Height(8);

            // Summary row: Amount in Words + Financial Breakdown Box
            col.Item().Row(row =>
            {
                // Left: Amount in Words & Notes
                row.RelativeItem(3).Column(wordsCol =>
                {
                    wordsCol.Item().Border(1).BorderColor("#E2E8F0").Padding(6).Column(inWords =>
                    {
                        inWords.Item().Text("Total Amount in Words:").Bold().FontSize(isA5 ? 7 : 8).FontColor("#475569");
                        inWords.Item().Text(doc.TotalAmountInWords).Bold().FontSize(isA5 ? 8 : 9).FontColor("#1E3A8A");
                    });

                    if (!string.IsNullOrWhiteSpace(doc.Notes))
                    {
                        wordsCol.Item().PaddingTop(4).Text($"Notes: {doc.Notes}").Italic().FontSize(isA5 ? 7 : 8);
                    }
                });

                row.ConstantItem(12);

                // Right: Financial Totals Box
                row.RelativeItem(2).Border(1).BorderColor("#CBD5E1").Column(totals =>
                {
                    void AddTotalLine(string label, decimal amount, bool bold = false, string? color = null)
                    {
                        totals.Item().PaddingHorizontal(6).PaddingVertical(2).Row(r =>
                        {
                            r.RelativeItem().Text(label).FontSize(isA5 ? 7 : 8);
                            var text = r.ConstantItem(75).AlignRight().Text($"₹ {amount:N2}");
                            if (bold) text.Bold();
                            if (color != null) text.FontColor(color);
                        });
                    }

                    AddTotalLine("Taxable Subtotal", doc.SubTotalTaxable);
                    if (isIntra)
                    {
                        AddTotalLine("CGST Total", doc.CgstTotal);
                        AddTotalLine("SGST Total", doc.SgstTotal);
                    }
                    else
                    {
                        AddTotalLine("IGST Total", doc.IgstTotal);
                    }
                    if (doc.CessTotal > 0)
                    {
                        AddTotalLine("Cess Total", doc.CessTotal);
                    }
                    if (doc.RoundOffAmount != 0)
                    {
                        AddTotalLine("Round Off", doc.RoundOffAmount);
                    }

                    totals.Item().LineHorizontal(1).LineColor("#1E3A8A");
                    totals.Item().Background("#EFF6FF").PaddingHorizontal(6).PaddingVertical(4).Row(r =>
                    {
                        r.RelativeItem().Text("GRAND TOTAL").Bold().FontSize(isA5 ? 9 : 10).FontColor("#1E3A8A");
                        r.ConstantItem(90).AlignRight().Text($"₹ {doc.GrandTotal:N2}").Bold().FontSize(isA5 ? 10 : 11).FontColor("#1E3A8A");
                    });
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, SalesDocumentDto doc, CompanyProfileDto company, bool isA5)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#CBD5E1");
            col.Item().PaddingTop(4).Row(row =>
            {
                // Bank Details
                row.RelativeItem(3).Column(bankCol =>
                {
                    bankCol.Item().Text("Settlement Bank Details:").Bold().FontSize(isA5 ? 7 : 8).FontColor("#1E3A8A");
                    if (!string.IsNullOrWhiteSpace(company.BankName))
                    {
                        bankCol.Item().Text($"Bank: {company.BankName} | A/C: {company.BankAccountNumber} | IFSC: {company.BankIfsc}");
                    }
                    if (!string.IsNullOrWhiteSpace(company.UpiId))
                    {
                        bankCol.Item().Text($"UPI ID: {company.UpiId}").Bold();
                    }
                    if (!string.IsNullOrWhiteSpace(doc.TermsAndConditions))
                    {
                        bankCol.Item().PaddingTop(2).Text($"Terms: {doc.TermsAndConditions}").FontSize(isA5 ? 6 : 7).FontColor("#64748B");
                    }
                });

                // Signatory Box
                row.RelativeItem(2).AlignRight().Column(sigCol =>
                {
                    sigCol.Item().Text($"For {company.TradeName ?? company.LegalName}:").Bold().FontSize(isA5 ? 7 : 8);
                    sigCol.Item().Height(isA5 ? 20 : 30);
                    sigCol.Item().Text("Authorized Signatory").FontSize(isA5 ? 7 : 8);
                });
            });

            col.Item().PaddingTop(4).AlignCenter().Text("This is a computer-generated invoice produced by BillMitra.").FontSize(isA5 ? 6 : 7).FontColor("#94A3B8");
        });
    }

    private static void ComposeThermalDocument(IDocumentContainer container, SalesDocumentDto doc, CompanyProfileDto company)
    {
        // 80mm POS Thermal Slip (Roll80 format)
        container.Page(page =>
        {
            page.ContinuousSize(72, Unit.Millimetre);
            page.Margin(3, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Lato"));

            page.Content().Column(col =>
            {
                col.Item().AlignCenter().Text(company.TradeName ?? company.LegalName).Bold().FontSize(11);
                col.Item().AlignCenter().Text($"GSTIN: {company.Gstin ?? "Unregistered"}").FontSize(8);
                col.Item().AlignCenter().Text($"{company.City} - {company.Pincode} | Ph: {company.Phone}").FontSize(7);
                col.Item().PaddingVertical(2).LineHorizontal(1);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text($"Inv: {doc.DocumentNumber ?? doc.DraftNumber}").Bold();
                    r.RelativeItem().AlignRight().Text($"{doc.DocumentDate:dd/MM/yy}");
                });
                col.Item().Text($"Customer: {doc.CustomerName}");
                if (!string.IsNullOrWhiteSpace(doc.CustomerGstin))
                {
                    col.Item().Text($"Cust GSTIN: {doc.CustomerGstin}");
                }
                col.Item().PaddingVertical(2).LineHorizontal(1);

                // Compact Items Table
                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4); // Item
                        c.ConstantColumn(24); // Qty
                        c.ConstantColumn(34); // Rate
                        c.ConstantColumn(38); // Amt
                    });

                    tbl.Header(h =>
                    {
                        h.Cell().Text("Item").Bold();
                        h.Cell().AlignRight().Text("Qty").Bold();
                        h.Cell().AlignRight().Text("Rate").Bold();
                        h.Cell().AlignRight().Text("Amt").Bold();
                    });

                    foreach (var itm in doc.Items)
                    {
                        tbl.Cell().Text(itm.ItemName);
                        tbl.Cell().AlignRight().Text($"{itm.Quantity:0.#}");
                        tbl.Cell().AlignRight().Text($"{itm.UnitPrice:0.#}");
                        tbl.Cell().AlignRight().Text($"{itm.TotalAmount:0.00}").Bold();
                    }
                });

                col.Item().PaddingVertical(2).LineHorizontal(1);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Taxable Subtotal:");
                    r.RelativeItem().AlignRight().Text($"₹ {doc.SubTotalTaxable:N2}");
                });
                if (doc.SupplyType == GstSupplyType.IntraState)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("CGST Total:");
                        r.RelativeItem().AlignRight().Text($"₹ {doc.CgstTotal:N2}");
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("SGST Total:");
                        r.RelativeItem().AlignRight().Text($"₹ {doc.SgstTotal:N2}");
                    });
                }
                else
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("IGST Total:");
                        r.RelativeItem().AlignRight().Text($"₹ {doc.IgstTotal:N2}");
                    });
                }
                if (doc.RoundOffAmount != 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Round Off:");
                        r.RelativeItem().AlignRight().Text($"₹ {doc.RoundOffAmount:N2}");
                    });
                }

                col.Item().PaddingVertical(1).LineHorizontal(1);
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("NET TOTAL:").Bold().FontSize(10);
                    r.RelativeItem().AlignRight().Text($"₹ {doc.GrandTotal:N2}").Bold().FontSize(11);
                });

                if (!string.IsNullOrWhiteSpace(company.UpiId))
                {
                    col.Item().PaddingTop(3).AlignCenter().Text($"Pay via UPI: {company.UpiId}").Bold().FontSize(8);
                }

                col.Item().PaddingTop(3).AlignCenter().Text("Thank You! Visit Again").Italic().FontSize(8);
                col.Item().AlignCenter().Text("Powered by BillMitra").FontSize(6).FontColor("#94A3B8");
            });
        });
    }

    public byte[] GenerateEWayBillPdf(EWayBillDto ewb, CompanyProfileDto company)
    {
        var pdfDoc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                page.Content().Element(c => ComposeEWayBillBody(c, ewb, company));
                page.Footer().Element(c => ComposeEWayBillFooter(c, ewb));
            });
        });

        return pdfDoc.GeneratePdf();
    }

    private static void ComposeEWayBillBody(IContainer container, EWayBillDto ewb, CompanyProfileDto company)
    {
        container.Column(col =>
        {
            // Top Government Header
            col.Item().Border(1).BorderColor("#CBD5E1").Padding(8).Column(hCol =>
            {
                hCol.Item().AlignCenter().Text("GOVERNMENT OF INDIA").Bold().FontSize(11).FontColor("#0F172A");
                hCol.Item().AlignCenter().Text("e-Way Bill System").Bold().FontSize(14).FontColor("#1E3A8A");
                hCol.Item().AlignCenter().Text("FORM GST EWB-01").Bold().FontSize(10).FontColor("#1E3A8A");
                hCol.Item().AlignCenter().Text("(See Rule 138 of the Central Goods and Services Tax Rules, 2017)").Italic().FontSize(8).FontColor("#64748B");
            });

            col.Item().PaddingVertical(4);

            // e-Way Bill Summary Box
            col.Item().Border(1).BorderColor("#1E3A8A").Padding(8).Column(summaryCol =>
            {
                summaryCol.Item().Row(r =>
                {
                    r.RelativeItem().Column(numCol =>
                    {
                        numCol.Item().Text("e-Way Bill Number").FontSize(8).FontColor("#64748B");
                        numCol.Item().Text(ewb.EWayBillNumber).Bold().FontSize(14).FontColor("#1E3A8A");
                    });

                    r.RelativeItem().AlignRight().Column(statusCol =>
                    {
                        statusCol.Item().Text("Status").FontSize(8).FontColor("#64748B");
                        statusCol.Item().Text($"[ {ewb.StatusName.ToUpperInvariant()} ]").Bold().FontSize(11).FontColor(ewb.Status == 3 ? "#DC2626" : "#059669");
                    });
                });

                summaryCol.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor("#E2E8F0");
                summaryCol.Item().PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("e-Way Bill Date: ").Bold();
                        t.Span(ewb.EWayBillDate.ToString("dd/MM/yyyy hh:mm tt"));
                    });

                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Generated By: ").Bold();
                        t.Span(ewb.FromGstin);
                    });
                });

                summaryCol.Item().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Valid From: ").Bold();
                        t.Span(ewb.EWayBillDate.ToString("dd/MM/yyyy hh:mm tt"));
                    });

                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Valid Until: ").Bold();
                        t.Span(ewb.ValidUntil.ToString("dd/MM/yyyy hh:mm tt"));
                    });
                });
            });

            col.Item().PaddingVertical(4);

            // PART-A Section
            col.Item().Border(1).BorderColor("#CBD5E1").Column(partA =>
            {
                partA.Item().Background("#1E3A8A").Padding(4).PaddingLeft(8).Text("PART - A").Bold().FontColor("#FFFFFF").FontSize(9);

                partA.Item().Padding(8).Column(fields =>
                {
                    RenderEwbField(fields, "1. GSTIN of Supplier", $"{ewb.FromGstin} - {ewb.FromName}");
                    RenderEwbField(fields, "2. Place of Dispatch", $"{company.City ?? "Dispatch Point"}, {company.StateName} ({company.StateCode}) - PIN: {company.Pincode ?? "N/A"}");
                    RenderEwbField(fields, "3. GSTIN of Recipient", string.IsNullOrWhiteSpace(ewb.ToGstin) ? $"URP (Unregistered) - {ewb.ToName}" : $"{ewb.ToGstin} - {ewb.ToName}");
                    RenderEwbField(fields, "4. Place of Delivery", $"{ewb.ToName}");
                    RenderEwbField(fields, "5. Document Details", $"{ewb.DocType} No: {ewb.DocNumber}, Dated: {ewb.DocDate:dd/MM/yyyy}");
                    RenderEwbField(fields, "6. Total Value of Goods", $"₹ {ewb.TotalInvoiceValue:N2}");
                    RenderEwbField(fields, "7. Reason for Transportation", "Outward Supply (Tax Invoice / Commercial Bill)");
                    RenderEwbField(fields, "8. Transporter Details", string.IsNullOrWhiteSpace(ewb.TransporterName) ? "Not Assigned / By Supplier" : $"{ewb.TransporterName} (ID: {ewb.TransporterId ?? "N/A"})");
                    RenderEwbField(fields, "9. Approximate Distance", $"{ewb.ApproximateDistanceKm} KMs");
                });
            });

            col.Item().PaddingVertical(4);

            // PART-B Section (Vehicle Details)
            col.Item().Border(1).BorderColor("#CBD5E1").Column(partB =>
            {
                partB.Item().Background("#1E3A8A").Padding(4).PaddingLeft(8).Text("PART - B (Vehicle & Conveyance Details)").Bold().FontColor("#FFFFFF").FontSize(9);

                partB.Item().Padding(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f); // Mode
                        columns.RelativeColumn(1.5f); // Vehicle No / Doc No
                        columns.RelativeColumn(1.2f); // From
                        columns.RelativeColumn(1.5f); // Entered Date
                        columns.RelativeColumn(1.5f); // Entered By
                        columns.RelativeColumn(1.0f); // Multi-Veh
                    });

                    // Table Header
                    table.Header(h =>
                    {
                        h.Cell().Background("#F1F5F9").Padding(4).Text("Mode").Bold().FontSize(8);
                        h.Cell().Background("#F1F5F9").Padding(4).Text("Vehicle / Doc No").Bold().FontSize(8);
                        h.Cell().Background("#F1F5F9").Padding(4).Text("From").Bold().FontSize(8);
                        h.Cell().Background("#F1F5F9").Padding(4).Text("Entered Date").Bold().FontSize(8);
                        h.Cell().Background("#F1F5F9").Padding(4).Text("Entered By").Bold().FontSize(8);
                        h.Cell().Background("#F1F5F9").Padding(4).Text("Multi-Veh").Bold().FontSize(8);
                    });

                    var modeName = ewb.TransportationMode switch
                    {
                        1 => "Road",
                        2 => "Rail",
                        3 => "Air",
                        4 => "Ship",
                        _ => "Road"
                    };

                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(modeName);
                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(ewb.VehicleNo ?? "Part-A Slip Only").Bold();
                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(company.City ?? "-");
                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(ewb.EWayBillDate.ToString("dd/MM/yyyy"));
                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(ewb.FromGstin);
                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text("No");
                });
            });

            col.Item().PaddingVertical(4);

            // Verification Barcode / Seal representation
            col.Item().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(verifyCol =>
            {
                verifyCol.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("||||| ||||||| |||||| |||||||||| ||||| |||||||| ||||| |||||||||| ||||").FontFamily("Courier New").FontSize(11).Bold();
                        c.Item().Text($"EWB: {ewb.EWayBillNumber} | INV: {ewb.DocNumber} | DATE: {ewb.EWayBillDate:dd-MMM-yyyy}").FontFamily("Courier New").FontSize(7).FontColor("#64748B");
                    });

                    r.ConstantItem(180).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("DIGITALLY SIGNED").Bold().FontSize(8).FontColor("#059669");
                        c.Item().AlignRight().Text("NIC e-Way Bill Gateway").FontSize(7).FontColor("#64748B");
                        c.Item().AlignRight().Text($"Generated: {ewb.EWayBillDate:yyyy-MM-dd HH:mm:ss}").FontSize(6).FontColor("#94A3B8");
                    });
                });
            });
        });
    }

    private static void RenderEwbField(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingVertical(2).Row(r =>
        {
            r.ConstantItem(160).Text(label).Bold().FontSize(8).FontColor("#475569");
            r.RelativeItem().Text(value).FontSize(8).FontColor("#0F172A");
        });
    }

    private static void ComposeEWayBillFooter(IContainer container, EWayBillDto ewb)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Statutory Notice: This e-Way Bill is valid under Rule 138 of CGST Rules, 2017. Physical signature not required.")
                .FontSize(7).FontColor("#64748B").Italic();
            row.RelativeItem().AlignRight().Text("BillMitra e-Way Bill Compliance System").FontSize(7).FontColor("#94A3B8");
        });
    }
}
